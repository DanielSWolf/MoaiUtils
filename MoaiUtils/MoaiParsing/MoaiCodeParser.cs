using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.Common;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;
using MoreLinq;

namespace MoaiUtils.MoaiParsing {
    public class MoaiCodeParser {
        private readonly Action<string> statusCallback;
        private Dictionary<string, MoaiType> typesByName;

        public MoaiCodeParser(Action<string> statusCallback) {
            this.statusCallback = statusCallback;
        }

        public WarningList Warnings { get; private set; }
        public string MoaiVersionInfo { get; private set; }

        public void Parse(DirectoryInfo moaiDirectory) {
            // Check that the input directory looks like the Moai main directory
            if (!moaiDirectory.GetDirectoryInfo(@"src\moai-core").Exists) {
                throw new PlainTextException(string.Format("Path '{0}' does not appear to be the base directory of a Moai source copy.", moaiDirectory));
            }

            // Initialize warning list
            Warnings = new WarningList();

            // Get Moai version
            MoaiVersionInfo = GetMoaiVersionInfo(moaiDirectory);
            statusCallback(string.Format("Found {0}.", MoaiVersionInfo));

            // Initialize type list with primitive types
            typesByName = new Dictionary<string, MoaiType>();
            var primitiveTypeNames = new[] { "nil", "boolean", "number", "string", "userdata", "function", "thread", "table" };
            foreach (string primitiveTypeName in primitiveTypeNames) {
                typesByName[primitiveTypeName] = new MoaiType { Name = primitiveTypeName, IsPrimitive = true };
            }

            // Parse Moai types and store them by type name
            statusCallback("Parsing Moai types.");
            ParseMoaiCodeFiles(moaiDirectory);

            // MOAILuaObject is not documented, probably because it would mess up
            // the Doxygen-generated documentation. Use dummy code instead.
            statusCallback("Adding hard-coded documentation for MoaiLuaObject base class.");
            FilePosition dummyFilePosition = new FilePosition(new FileInfo("MoaiLuaObject dummy code"));
            ParseMoaiCodeFile(MoaiLuaObject.DummyCode, dummyFilePosition);

            // Make sure every class directly or indirectly inherits from MOAILuaObject
            MoaiType moaiLuaObjectType = GetOrCreateType("MOAILuaObject", null);
            foreach (MoaiType type in typesByName.Values) {
                if (!(type.AncestorTypes.Contains(moaiLuaObjectType)) && type != moaiLuaObjectType) {
                    type.BaseTypes.Add(moaiLuaObjectType);
                }
            }

            // Check if we have information on all referenced classes
            IEnumerable<MoaiType> typesReferencedInDocumentation = typesByName.Values
                .Where(type => type.DocumentationReferences.Any());
            foreach (MoaiType type in typesReferencedInDocumentation.ToArray()) {
                WarnIfSpeculative(type);
            }

            statusCallback("Creating compact method signatures.");
            foreach (MoaiType type in typesByName.Values) {
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

        public IEnumerable<MoaiType> DocumentedTypes {
            get {
                return typesByName.Values
                    .Where(type => type.IsDocumented);
            }
        }

        private string GetMoaiVersionInfo(DirectoryInfo moaiDirectory) {
            var versionFileInfo = moaiDirectory.GetFileInfo(@"src\config-default\moai_version.h");
            try {
                // Read version file
                string versionText = File.ReadAllText(versionFileInfo.FullName);

                // Extract version, revision, and author
                string version = Regex.Match(versionText, @"MOAI_SDK_VERSION_MAJOR_MINOR\s+([0-9.]+)").Groups[1].Value;
                int revision = int.Parse(Regex.Match(versionText, @"MOAI_SDK_VERSION_REVISION\s+(-?[0-9]+)").Groups[1].Value, CultureInfo.InvariantCulture);
                string author = Regex.Match(versionText, @"MOAI_SDK_VERSION_AUTHOR\s+""(.*?)""").Groups[1].Value;

                // Build version string
                string result = string.Format("Moai SDK {0}", version);
                if (revision >= 0) {
                    result += string.Format(" revision {0}", revision);
                }
                if (author != string.Empty) {
                    result += string.Format(revision > 1 ? " ({0})" : " (interim version by {0})", author);
                }

                return result;
            } catch (Exception e) {
                Warnings.Add(new FilePosition(versionFileInfo), WarningType.ToolLimitation,
                    "Error determining Moai version: {0}", e.Message);
                return "Moai SDK (unknown version)";
            }
        }

        private readonly Dictionary<string, string> luaTypeNames = new Dictionary<string, string> {
            // Synonyms for boolean
            { "bool", "boolean" },
            { "cpBool", "boolean" },

            // Synonyms for number
            { "num", "number" },
            { "int", "number" },
            { "u8", "number" },
            { "u16", "number" },
            { "u32", "number" },
            { "u64", "number" },
            { "s8", "number" },
            { "s16", "number" },
            { "s32", "number" },
            { "s64", "number" },
            { "integer", "number" },
            { "float", "number" },
            { "double", "number" },
            { "cpFloat", "number" },
            { "size_t", "number" },
            { "cpCollisionType", "number" },
            { "cpGroup", "number" },
            { "cpLayers", "number" },
            { "cpTimestamp", "number" },

            // Synonyms for string
            { "cc8*", "string" }
        };

        private void WarnIfSpeculative(MoaiType type) {
            if (type.Name == "...") return;

            if (type.Name.EndsWith("...")) {
                type = GetOrCreateType(type.Name.Substring(0, type.Name.Length - 3), null);
            }

            if (!type.IsDocumented && !type.IsPrimitive) {
                // Make an educated guess as to what type was meant.
                string nameProposal;
                if (luaTypeNames.ContainsKey(type.Name)) {
                    nameProposal = luaTypeNames[type.Name];
                } else {
                    nameProposal = typesByName
                        .Where(pair => pair.Value.IsDocumented || pair.Value.IsPrimitive)
                        .Select(pair => pair.Key)
                        .MinBy(name => Levenshtein.Distance(name, type.Name));
                    if (Levenshtein.Similarity(nameProposal, type.Name) < 0.6) {
                        nameProposal = null;
                    }
                }

                foreach (FilePosition referencingFilePosition in type.DocumentationReferences) {
                    string message = string.Format(
                        "Documentation mentions missing or undocumented type '{0}'.", type.Name);
                    if (nameProposal != null) {
                        message += string.Format(" Should this be '{0}'?", nameProposal);
                    }
                    Warnings.Add(referencingFilePosition, WarningType.UnexpectedValue, message);
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

        private void ParseMoaiCodeFiles(DirectoryInfo moaiDirectory) {
            IEnumerable<FileInfo> codeFiles = Directory
                .EnumerateFiles(moaiDirectory.GetDirectoryInfo("src").FullName, "*.*", SearchOption.AllDirectories)
                .Where(name => name.EndsWith(".cpp") || name.EndsWith(".h"))
                .Select(name => new FileInfo(name));

            foreach (var codeFile in codeFiles) {
                ParseMoaiCodeFile(codeFile, new FilePosition(codeFile));
            }
        }

        private static readonly Regex documentationRegex = new Regex(@"
            /\*\*\s*
                # Documentation
                (?<annotation>(?<!\S)@[\s\S]*?)+
            \*/\s*
            (
                # Class definition
                (class|struct)\s+
                (?<className>[A-Za-z0-9_]+)\s*
                (
                    :\s*
                    (
                        ((public|protected|private|virtual)\s*)+
                        (?<baseClassName>[A-Za-z0-9_:<,\s>]+?)\s*
                        ,?\s*
                    )+
                    {
                )?
                |
                # Method definition
                int\s+(?<className>[A-Za-z0-9_]+)\s*::\s*(?<methodName>[A-Za-z0-9_]+)\s*\([^)]*\)\s*
                (
                    \{(?<methodBody>[\s\S]*?)^\}
                )?
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private MoaiType GetOrCreateType(string typeName, FilePosition documentationPosition) {
            MoaiType result = typesByName.ContainsKey(typeName)
                ? typesByName[typeName]
                : typesByName[typeName] = new MoaiType { Name = typeName };
            if (documentationPosition != null) {
                result.DocumentationReferences.Add(documentationPosition);
            }
            return result;
        }

        private void ParseMoaiCodeFile(FileInfo codeFile, FilePosition filePosition) {
            string code = File.ReadAllText(codeFile.FullName);
            ParseMoaiCodeFile(code, filePosition);
        }

        private void ParseMoaiCodeFile(string code, FilePosition filePosition) {
            // Find all documentation blocks
            var matches = documentationRegex.Matches(code);

            // Parse documentation blocks
            foreach (Match match in matches) {
                // A documentation block may be attached to a type or to a method.
                string typeName = match.Groups["className"].Value;
                FilePosition documentationPosition = match.Groups["methodName"].Success
                    ? new MethodPosition(filePosition, typeName, match.Groups["methodName"].Value)
                    : new TypePosition(filePosition, typeName);

                // Parse annotations, filtering out unknown ones
                Annotation[] annotations = match.Groups["annotation"].Captures
                    .Cast<Capture>()
                    .Select(capture => Annotation.Create(capture.Value, documentationPosition, Warnings))
                    .ToArray();
                foreach (var unknownAnnotation in annotations.OfType<UnknownAnnotation>()) {
                    Warnings.Add(documentationPosition, WarningType.UnexpectedAnnotation,
                        "Unknown annotation command '{0}'.", unknownAnnotation.Command);
                }
                annotations = annotations
                    .Where(annotation => !(annotation is UnknownAnnotation))
                    .ToArray();

                // Parse annotation block
                MoaiType type = GetOrCreateType(typeName, documentationPosition);
                if (documentationPosition is MethodPosition) {
                    // The documentation was attached to a method definition

                    // Get method body
                    string methodBody = match.Groups["methodBody"].Value;

                    ParseMethodDocumentation(type, annotations, methodBody, (MethodPosition) documentationPosition);
                } else {
                    // The documentation was attached to a type definition

                    // Get base type names, ignoring all template classes
                    MoaiType[] baseTypes = match.Groups["baseClassName"].Captures
                        .Cast<Capture>()
                        .Where(capture => !capture.Value.Contains("<"))
                        .Select(capture => GetOrCreateType(capture.Value, null))
                        .ToArray();

                    var typePosition = (TypePosition) documentationPosition;
                    type.TypePosition = typePosition;
                    ParseTypeDocumentation(type, annotations, baseTypes, typePosition, Warnings);
                }
            }
        }

        private void ParseTypeDocumentation(MoaiType type, Annotation[] annotations, MoaiType[] baseTypes, TypePosition typePosition, WarningList warnings) {
            // Check that there is a single @name annotation
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                warnings.Add(typePosition, WarningType.MissingAnnotation, "Missing @name annotation.");
            } else if (nameAnnotationCount > 1) {
                warnings.Add(typePosition, WarningType.UnexpectedAnnotation, "Multiple @name annotations.");
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                warnings.Add(typePosition, WarningType.MissingAnnotation, "Missing @text annotation.");
            } else if (textAnnotationCount > 1) {
                warnings.Add(typePosition, WarningType.UnexpectedAnnotation, "Multiple @text annotations.");
            }

            // Store base types
            type.BaseTypes.AddRange(baseTypes);

            // Parse annotations
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Nothing to do. Name is already set. Just make sure the annotation is correct.
                    var nameAnnotation = (NameAnnotation) annotation;
                    if (nameAnnotation.Value != type.Name) {
                        warnings.Add(typePosition, WarningType.UnexpectedValue,
                            "@name annotation has inconsistent value '{0}'. Expected '{1}'.", nameAnnotation.Value, type.Name);
                    }
                } else if (annotation is TextAnnotation) {
                    // Set type description
                    type.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is FieldAnnotation) {
                    // Add field (constant, flag, or attribute)
                    var fieldAnnotation = (FieldAnnotation) annotation;
                    MoaiField field = (annotation is ConstantAnnotation) ? new MoaiConstant()
                        : (annotation is FlagAnnotation) ? (MoaiField) new MoaiFlag()
                        : new MoaiAttribute();
                    field.OwningType = type;
                    field.Name = fieldAnnotation.Name;
                    field.Description = fieldAnnotation.Description;
                    type.Members.Add(field);
                } else {
                    warnings.Add(typePosition, WarningType.UnexpectedAnnotation,
                        "Unexpected {0} annotation.", annotation.Command);
                }
            }
        }

        private static readonly Regex paramAccessRegex = new Regex(
            @"state\s*\.\s*(GetLuaObject|GetValue)\s*<\s*(?<type>[A-Za-z0-9_*]+)\s*>\s*\(\s*(?<index>[0-9]+)\s*,",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private void ParseMethodDocumentation(MoaiType type, Annotation[] annotations, string methodBody, MethodPosition methodPosition) {
            // Check that there is a single @name annotation and that it isn't a duplicate. Otherwise exit.
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                Warnings.Add(methodPosition, WarningType.MissingAnnotation, "Missing @name annotation.");
                return;
            }
            if (nameAnnotationCount > 1) {
                Warnings.Add(methodPosition, WarningType.UnexpectedAnnotation, "Multiple @name annotations.");
            }
            var nameAnnotation = annotations.OfType<NameAnnotation>().Single();
            if (type.Members.Any(member => member.Name == nameAnnotation.Value)) {
                Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "There is already a member with name '{0}'.", nameAnnotation.Value);
                return;
            }

            // Check that @name annotation sticks to convention
            if (!methodPosition.NativeMethodName.StartsWith("_")) {
                Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "Unexpected C++ method name '{0}'. By convention, the name of a Lua method implementation shold start with an underscore.",
                    methodPosition.NativeMethodName);
            }
            string expectedName = methodPosition.NativeMethodName.Substring(1);
            if (nameAnnotation.Value != expectedName) {
                Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "@name annotation has unexpected value '{0}'. By convention expected '{1}'.",
                    nameAnnotation.Value, expectedName);
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                Warnings.Add(methodPosition, WarningType.MissingAnnotation, "Missing @text annotation.");
            } else if (textAnnotationCount > 1) {
                Warnings.Add(methodPosition, WarningType.UnexpectedAnnotation, "Multiple @text annotations.");
            }

            // Check that there is at least one @out annotation
            if (!annotations.OfType<OutParameterAnnotation>().Any()) {
                Warnings.Add(methodPosition, WarningType.MissingAnnotation,
                    "Missing @out annotation. Even for void methods, an @out annotation with type nil is expected.");
            }

            // Parse annotations
            // Guess if the method is static
            bool isStatic = annotations
                .OfType<InParameterAnnotation>()
                .All(param => param.Name != "self");
            var method = new MoaiMethod {
                MethodPosition = methodPosition,
                Name = nameAnnotation.Value,
                OwningType = type,
                IsStatic = isStatic
            };
            type.Members.Add(method);
            MoaiMethodOverload currentOverload = null;
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Nothing to do - name has already been set.
                } else if (annotation is TextAnnotation) {
                    // Set method description
                    method.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is ParameterAnnotation) {
                    if (currentOverload == null) {
                        currentOverload = new MoaiMethodOverload { OwningMethod = method };
                        method.Overloads.Add(currentOverload);
                    }
                    var parameterAnnotation = (ParameterAnnotation) annotation;
                    string paramName = parameterAnnotation.Name;
                    if (annotation is InParameterAnnotation | annotation is OptionalInParameterAnnotation) {
                        // Add input parameter
                        if (currentOverload.InParameters.Any(param => param.Name == paramName)) {
                            Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                                "Found multiple params with name '{0}' for single overload.", paramName);
                        }
                        var inParameter = new MoaiInParameter {
                            Name = paramName,
                            Description = parameterAnnotation.Description,
                            Type = GetOrCreateType(parameterAnnotation.Type, methodPosition),
                            IsOptional = annotation is OptionalInParameterAnnotation
                        };
                        currentOverload.InParameters.Add(inParameter);
                    } else {
                        // Add output parameter
                        var outParameter = new MoaiOutParameter {
                            Name = paramName,
                            Type = GetOrCreateType(parameterAnnotation.Type, methodPosition),
                            Description = parameterAnnotation.Description
                        };
                        currentOverload.OutParameters.Add(outParameter);
                    }
                } else if (annotation is OverloadAnnotation) {
                    // Let the next parameter annotation start a new override
                    currentOverload = null;
                } else {
                    Warnings.Add(methodPosition, WarningType.UnexpectedAnnotation,
                        "Unexpected {0} annotation.", annotation.Command);
                }
            }

            // Make sure that no required parameter follows an optional one
            foreach (MoaiMethodOverload overload in method.Overloads) {
                if (overload.InParameters.Pairwise().Any(pair => pair.Item1.IsOptional && !pair.Item2.IsOptional)) {
                    Warnings.Add(methodPosition, WarningType.UnexpectedAnnotation,
                        "Required params (@in) are not allowed after optional params (@opt).");
                }
            }

            // Check 'self' params
            foreach (var overload in method.Overloads) {
                if (overload.InParameters.Skip(1).Any(param => param.Name == "self")) {
                    Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                        "'self' param must be at index 1.");
                }
                var firstInParam = overload.InParameters.FirstOrDefault();
                if (firstInParam != null && firstInParam.Name == "self") {
                    if (firstInParam.Type != method.OwningType) {
                        Warnings.Add(methodPosition, WarningType.UnexpectedValue,
                            "'self' param is of type {0}. Expected {1}.",
                            firstInParam.Type != null ? firstInParam.Type.Name : "unknown", method.OwningType.Name);
                    }
                }
            }

            // Analyze body to find undocumented overloads
            var matches = paramAccessRegex.Matches(methodBody);
            foreach (Match match in matches) {
                string paramTypeName = match.Groups["type"].Value;
                if (luaTypeNames.ContainsKey(paramTypeName)) {
                    paramTypeName = luaTypeNames[paramTypeName];
                }
                int index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
                bool isDocumented = method.Overloads.Any(overload => {
                    if (overload.InParameters.Count < index) return false;
                    var param = overload.InParameters[index - 1];
                    return param.Type.Name == paramTypeName;
                });
                if (!isDocumented) {
                    Warnings.Add(methodPosition, WarningType.MissingAnnotation,
                        "Missing documentation for parameter #{0} of type {1}.", index, paramTypeName);
                }
            }
        }
    }
}