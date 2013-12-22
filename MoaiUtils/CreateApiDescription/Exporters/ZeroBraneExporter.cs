using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MoaiUtils.Common;
using MoaiUtils.CreateApiDescription.CodeGraph;
using MoaiUtils.LuaIO;
using MoaiUtils.Tools;

namespace MoaiUtils.CreateApiDescription.Exporters {
    public class ZeroBraneExporter : IApiExporter {
        public void Export(IEnumerable<MoaiType> types, DirectoryInfo outputDirectory) {
            LuaTable typeListTable = CreateTypeListTable(types);
            var targetFileInfo = outputDirectory.GetFileInfo("moai.lua");
            var commentLines = new[] {
                "Documentation of the Moai SDK (http://getmoai.com/)",
                string.Format(CultureInfo.InvariantCulture, "Generated on {0:d} by {1}", DateTime.Now, CurrentUtility.Signature),
                CurrentUtility.MoaiUtilsHint
            };
            var headComment = new LuaComment(commentLines, blankLineAfter: true);
            LuaTableWriter.Write(typeListTable, targetFileInfo, headComment);
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
                { "description", type.Description },
                { "childs", CreateMemberListTable(type) }
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
                { "description", field.Description }
            };
        }

        private LuaTable CreateMemberTable(MoaiMethod method) {
            StringBuilder description = new StringBuilder();
            description.AppendLine(method.Description);
            if (method.Overloads.Count == 1) {
                description.Append(GetOverloadInfo(method.Overloads.Single()));
            } else if (method.Overloads.Any()) {
                foreach (var overload in method.Overloads) {
                    description.AppendLine();
                    description.AppendLine("Overload:");
                    description.Append(GetOverloadInfo(overload));
                }
            }

            var memberTable = new LuaTable {
                { "type", method.IsStatic ? "function" : "method" },
                { "description", description.ToString().Trim() },
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
                result.Append("in: ");
                AppendParameterInfo(result, inParameter);
                if (inParameter.IsOptional) {
                    result.Append("]");
                }
                result.AppendLine();
            }
            foreach (var outParameter in overload.OutParameters) {
                result.Append("out: ");
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
    }
}