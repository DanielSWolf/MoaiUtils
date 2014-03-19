using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.Common;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing {
    public class MoaiParser {
        private readonly Action<string> statusCallback;
        private MoaiTypeCollection types;

        public MoaiParser(Action<string> statusCallback) {
            this.statusCallback = statusCallback;
        }

        public WarningList Warnings { get; private set; }
        public MoaiVersionInfo MoaiVersionInfo { get; private set; }

        public void Parse(DirectoryInfo moaiDirectory) {
            // Check that the input directory looks like the Moai main directory
            if (!moaiDirectory.GetDirectoryInfo(@"src\moai-core").Exists) {
                throw new PlainTextException(string.Format("Path '{0}' does not appear to be the base directory of a Moai source copy.", moaiDirectory));
            }

            // Initialize warning list
            Warnings = new WarningList();

            // Get Moai version
            MoaiVersionInfo = new MoaiVersionInfo(moaiDirectory);
            statusCallback(string.Format("Found {0}.", MoaiVersionInfo));

            // Initialize type list with primitive types
            types = new MoaiTypeCollection(initializeWithPrimitives: true);

            // Parse Moai types and store them by type name
            statusCallback("Parsing Moai types.");
            ParseMoaiCodeFiles(moaiDirectory);

            // MOAILuaObject is not documented, probably because it would mess up
            // the Doxygen-generated documentation. Use dummy code instead.
            statusCallback("Adding hard-coded documentation for MoaiLuaObject base class.");
            FilePosition dummyFilePosition = new FilePosition(new FileInfo("MoaiLuaObject dummy code"));
            MoaiFileParser.ParseMoaiCodeFile(MoaiLuaObject.DummyCode, dummyFilePosition, types, Warnings);

            // Make sure every class directly or indirectly inherits from MOAILuaObject
            MoaiType moaiLuaObjectType = types.GetOrCreate("MOAILuaObject", null);
            foreach (MoaiType type in types) {
                if (!(type.AncestorTypes.Contains(moaiLuaObjectType)) && type != moaiLuaObjectType) {
                    type.BaseTypes.Add(moaiLuaObjectType);
                }
            }

            // Mark registered classes as scriptable
            statusCallback("Checking which types are registered to be scriptable from Lua.");
            MarkScriptableClasses(moaiDirectory);

            // Check if we have information on all referenced classes
            IEnumerable<MoaiType> typesReferencedInDocumentation = types
                .Where(type => type.DocumentationReferences.Any());
            foreach (MoaiType type in typesReferencedInDocumentation.ToArray()) {
                WarnIfSpeculative(type);
            }

            statusCallback("Creating compact method signatures.");
            CreateCompactMethodSignatures();
        }

        public IEnumerable<MoaiType> DocumentedTypes {
            get { return types.Where(type => type.IsDocumented); }
        }

        private void ParseMoaiCodeFiles(DirectoryInfo moaiDirectory) {
            // Parse .cpp and .h files in src
            string srcDirPath = moaiDirectory.GetDirectoryInfo("src").FullName;
            IEnumerable<FileInfo> codeFiles = Directory.EnumerateFiles(srcDirPath, "*.*", SearchOption.AllDirectories)
                .Where(name => name.EndsWith(".cpp") || name.EndsWith(".h"))
                .Select(name => new FileInfo(name));

            foreach (var codeFile in codeFiles) {
                MoaiFileParser.ParseMoaiCodeFile(codeFile, new FilePosition(codeFile), types, Warnings);
            }
        }

        private void WarnIfSpeculative(MoaiType type) {
            if (type.Name == "...") return;

            if (type.Name.EndsWith("...")) {
                type = types.GetOrCreate(type.Name.Substring(0, type.Name.Length - 3), null);
            }

            if (!type.IsDocumented && !type.IsPrimitive) {
                // Make an educated guess as to what type was meant.
                MoaiType typeProposal = types.Find(type.Name,
                    MatchMode.FindSynonyms | MatchMode.FindSimilar,
                    t => t.IsDocumented || t.IsPrimitive);

                foreach (FilePosition referencingFilePosition in type.DocumentationReferences) {
                    string message = string.Format(
                        "Documentation mentions missing or undocumented type '{0}'.", type.Name);
                    if (typeProposal != null) {
                        message += string.Format(" Should this be '{0}'?", typeProposal.Name);
                    }
                    Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
                }
            }
        }

        private void CreateCompactMethodSignatures() {
            foreach (MoaiType type in types) {
                foreach (MoaiMethod method in type.Members.OfType<MoaiMethod>()) {
                    if (!method.Overloads.Any()) {
                        Warnings.Add(method.MethodPosition, WarningType.MissingAnnotation,
                            "No method documentation found.");
                        continue;
                    }

                    try {
                        method.InParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.InParameters.ToArray()));
                        method.OutParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.OutParameters.ToArray()));
                    } catch (Exception e) {
                        Warnings.Add(method.MethodPosition, WarningType.ToolLimitation,
                            "Error determining method signature. {0}", e.Message);
                    }
                }
            }
        }

        private static ISignature GetCompactSignature(IEnumerable<MoaiParameter[]> overloads) {
            List<Parameter[]> parameterOverloads = new List<Parameter[]>();
            foreach (MoaiParameter[] overload in overloads) {
                // Input parameters may be optional. In these cases, create multiple overloads.
                for (int index = overload.Length - 1;
                    index >= 0 && overload[index] is MoaiInParameter && ((MoaiInParameter) overload[index]).IsOptional;
                    index--) {
                    parameterOverloads.Add(ConvertOverload(overload.Take(index)).ToArray());
                }
                parameterOverloads.Add(ConvertOverload(overload).ToArray());
            }
            return CompactSignature.FromOverloads(parameterOverloads.ToArray());
        }

        private static IEnumerable<Parameter> ConvertOverload(IEnumerable<MoaiParameter> overload) {
            return overload.Select(parameter => new Parameter { Name = parameter.Name, Type = parameter.Type.Name, ShowName = true });
        }

        private static readonly Regex classRegistrationInLuaRegex = new Regex(
           @"\.extend\s*\(\s*['""](?<className>[A-Za-z0-9_]+)['""]\s*,",
           RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private static readonly Regex classRegistrationInCPlusPlusRegex = new Regex(
            @"(?<!//\s*)REGISTER_LUA_CLASS\s*\(\s*(?<className>[A-Za-z0-9_]+)\s*\)",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private void MarkScriptableClasses(DirectoryInfo moaiDirectory) {
            IEnumerable<string> fileNames = Directory.EnumerateFiles(moaiDirectory.FullName, "*.*", SearchOption.AllDirectories);
            foreach (string fileName in fileNames) {
                // Determine file type
                Regex registrationRegex;
                if (fileName.EndsWith(".cpp") || fileName.EndsWith(".h") || fileName.EndsWith(".mm")) {
                    registrationRegex = classRegistrationInCPlusPlusRegex;
                } else if (fileName.EndsWith(".lua")) {
                    registrationRegex = classRegistrationInLuaRegex;
                } else {
                    continue;
                }

                // Search file for type registrations
                var matches = registrationRegex.Matches(File.ReadAllText(fileName));
                foreach (Match match in matches) {
                    string typeName = match.Groups["className"].Value;
                    MoaiType type = types.GetOrCreate(typeName, new FilePosition(new FileInfo(fileName)));
                    type.IsScriptable = true;
                }
            }
        }

    }
}