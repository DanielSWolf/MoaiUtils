using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoaiUtils.LuaIO;
using MoaiUtils.MoaiParsing;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.DocExport.Exporters {
    public class ZeroBraneExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, string header, DirectoryInfo outputDirectory) {
            // Create contents
            LuaTable typeListTable = CreateTypeListTable(types);

            // Write to file
            var targetFileInfo = outputDirectory.GetFileInfo("moai.lua");
            LuaTableWriter.Write(typeListTable, targetFileInfo, new LuaComment(header, blankLineAfter: true));
        }

        private LuaTable CreateTypeListTable(IEnumerable<MoaiType> types) {
            var typeListTable = new LuaTable();
            foreach (MoaiType type in types.OrderBy(t => t.Name)) {
                typeListTable.Add(new LuaComment(type.Signature, blankLineBefore: typeListTable.Any()));
                typeListTable.Add(type.Name, CreateTypeTable(type));
            }
            return typeListTable;
        }

        private LuaTable CreateTypeTable(MoaiType type) {
            return new LuaTable {
                { "type", "class" },
                { "description", ConvertString(type.Description) },
                { "childs" /* sic */, CreateMemberListTable(type) }
            };
        }

        private LuaTable CreateMemberListTable(MoaiType type) {
            var memberListTable = new LuaTable();

            memberListTable.Add(new LuaComment("Direct members"));
            IEnumerable<MoaiTypeMember> directMembers = type.Members
                .OrderBy(member => member.GetType().Name) // MoaiAttribute, then MoaiConstant, MoaiFlag, MoaiMethod
                .ThenBy(member => member.Name);
            foreach (var member in directMembers) {
                memberListTable.Add(member.Name, CreateMemberTable((dynamic) member));
            }

            if (type.InheritedMembers.Any()) {
                memberListTable.Add(new LuaComment("Inherited members", blankLineBefore: true));
                var inheritedMembers = type.InheritedMembers
                    .OrderBy(member => member.GetType().Name)
                    .ThenBy(member => member.Name);
                foreach (var member in inheritedMembers) {
                    memberListTable.Add(member.Name, CreateMemberTable((dynamic) member));
                }
            }

            return memberListTable;
        }

        private LuaTable CreateMemberTable(MoaiField field) {
            return new LuaTable {
                { "type", "value" },
                { "description", ConvertString(field.Description) }
            };
        }

        private LuaTable CreateMemberTable(MoaiMethod method) {
            StringBuilder description = new StringBuilder();
            description.AppendLine(method.Description);
            if (method.Overloads.Count == 1) {
                description.AppendLine();
                description.Append(GetOverloadInfo(method.Overloads.Single()));
            } else if (method.Overloads.Any()) {
                foreach (var overload in method.Overloads) {
                    description.AppendLine();
                    description.AppendLine("Overload:");
                    description.Append(GetOverloadInfo(overload));
                }
            }

            var memberTable = new LuaTable {
                { "type", method.Overloads.Any(overload => overload.IsStatic) ? "function" : "method" },
                { "description", ConvertString(description.ToString().Trim()) },
                { "args", method.InParameterSignature != null ? method.InParameterSignature.ToString(SignatureGrouping.Any) : null },
                { "returns", method.OutParameterSignature != null ? method.OutParameterSignature.ToString(SignatureGrouping.Any) : null },
                { "valuetype", GetValueType(method)}
            };

            return memberTable;
        }

        private static string GetValueType(MoaiMethod method) {
            MoaiOutParameter[] outParameters = method.Overloads
                .Where(overload => overload.OutParameters.Any())
                .Select(overload => overload.OutParameters.First())
                .ToArray();
            
            // There must be overloads defining a (first) return type
            if (!outParameters.Any()) return null;

            // All overloads must have the same return type
            if (outParameters.Any(outParameter => outParameter.Type != outParameters.First().Type)) return null;
            
            // nil doesn't count.
            string valueType = outParameters.First().Type.Name;
            if (valueType == "nil") return null;

            return valueType;
        }

        private static string GetOverloadInfo(MoaiMethodOverload overload) {
            StringBuilder result = new StringBuilder();
            foreach (var inParameter in overload.InParameters) {
                if (inParameter.IsOptional) {
                    result.Append("[");
                }
                result.Append("–> ");
                AppendParameterInfo(result, inParameter);
                if (inParameter.IsOptional) {
                    result.Append("]");
                }
                result.AppendLine();
            }
            foreach (var outParameter in overload.OutParameters) {
                result.Append("<– ");
                AppendParameterInfo(result, outParameter);
                result.AppendLine();
            }
            return result.ToString();
        }

        private static void AppendParameterInfo(StringBuilder result, MoaiParameter parameter) {
            result.Append(parameter.Type.Name);
            if (parameter.Name != null) {
                result.AppendFormat(" {0}", parameter.Name);
            }
            if (parameter.Description != null) {
                result.AppendFormat(": {0}", parameter.Description);
            }
        }

        private static readonly Regex newlineRegex = new Regex(@"\r\n?|\n", RegexOptions.Compiled);

        // For whatever reason, ZeroBrane expects line breaks to be marked with "<br>".
        // It doesn't understand any other HTML, though.
        private static string ConvertString(string s) {
            if (s == null) return null;
            return newlineRegex.Replace(s, "<br>");
        }
    }
}