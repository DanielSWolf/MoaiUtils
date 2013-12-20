using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CreateApiDescription.CodeGraph;
using log4net;
using Tools;

namespace CreateApiDescription {
    public class MoaiCodeParser {
        private static readonly ILog log = LogManager.GetLogger(typeof(MoaiCodeParser));

        private Dictionary<string, MoaiType> typesByName = new Dictionary<string, MoaiType>();

        public void Parse(DirectoryInfo moaiSourceDirectory) {
            // Check that the input directory looks like the Moai src directory
            if (!moaiSourceDirectory.GetDirectoryInfo("moai-core").Exists) {
                throw new ApplicationException(string.Format("Path '{0}' does not appear to be the 'src' directory of a Moai source copy.", moaiSourceDirectory));
            }

            // Parse Moai types and store them by type name
            log.Info("Parsing Moai types.");
            typesByName = new Dictionary<string, MoaiType>();
            ParseMoaiCodeFiles(moaiSourceDirectory);

            // MOAILuaObject is not documented, probably because it would mess up
            // the Doxygen-generated documentation. Use dummy code instead.
            log.Info("Adding hard-coded documentation for MoaiLuaObject base class.");
            ParseMoaiCode(MoaiLuaObject.DummyCode, "in MoaiLuaObject dummy code");

            // Make sure every class directly or indirectly inherits from MOAILuaObject
            MoaiType moaiLuaObjectType = GetOrCreateType("MOAILuaObject");
            foreach (MoaiType type in typesByName.Values) {
                if (!(type.AncestorTypes.Contains(moaiLuaObjectType)) && type != moaiLuaObjectType) {
                    type.BaseTypes.Add(moaiLuaObjectType);
                }
            }

            log.Info("Creating compact method signatures.");
            foreach (MoaiType type in typesByName.Values) {
                foreach (MoaiMethod method in type.Members.OfType<MoaiMethod>()) {
                    if (!method.Overloads.Any()) {
                        log.WarnFormat("No documentation found for method {0}.", method);
                        continue;
                    }

                    try {
                        method.InParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.InParameters.ToArray()));
                        method.OutParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.OutParameters.ToArray()));
                    } catch (Exception e) {
                        log.WarnFormat("Error determining signature for method {0}. {1}", method, e.Message);
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

        public IEnumerable<MoaiType> Types {
            get {
                return typesByName.Values
                    .Where(type => type.Description != null || type.Members.Any());
            }
        }

        private void ParseMoaiCodeFiles(DirectoryInfo moaiSourceDirectory) {
            IEnumerable<FileInfo> codeFiles = Directory
                .EnumerateFiles(moaiSourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
                .Where(name => name.EndsWith(".cpp") || name.EndsWith(".h"))
                .Select(name => new FileInfo(name));

            foreach (var codeFile in codeFiles) {
                string context = string.Format("in {0}", codeFile.RelativeTo(moaiSourceDirectory));
                ParseMoaiCodeFile(codeFile, context);
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
                int\s+(?<className>[A-Za-z0-9_]+)\s*::\s*(?<methodName>[A-Za-z0-9_]+)
            )", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private MoaiType GetOrCreateType(string typeName) {
            if (!typesByName.ContainsKey(typeName)) {
                typesByName[typeName] = new MoaiType { Name = typeName };
            }
            return typesByName[typeName];
        }

        private void ParseMoaiCodeFile(FileInfo codeFile, string context) {
            string code = File.ReadAllText(codeFile.FullName);
            ParseMoaiCode(code, context);
        }

        private void ParseMoaiCode(string code, string context) {
            // Find all documentation blocks
            var matches = documentationRegex.Matches(code);

            foreach (Match match in matches) {
                string typeName = match.Groups["className"].Value;
                MoaiType type = GetOrCreateType(typeName);

                // Parse annotations, filtering out unknown ones
                Annotation[] annotations = match.Groups["annotation"].Captures
                    .Cast<Capture>()
                    .Select(capture => Annotation.Create(capture.Value))
                    .ToArray();
                foreach (var unknownAnnotation in annotations.OfType<UnknownAnnotation>()) {
                    log.WarnFormat("Unknown annotation {0} {1}.", unknownAnnotation.Command, context);
                }
                annotations = annotations
                    .Where(annotation => !(annotation is UnknownAnnotation))
                    .ToArray();

                if (match.Groups["methodName"].Success) {
                    // The documentation was attached to a method definition
                    string methodName = match.Groups["methodName"].Value;
                    string methodContext = string.Format("for {0}::{1}() {2}", typeName, methodName, context);
                    ParseMethodDocumentation(type, annotations, methodContext);
                } else {
                    // The documentation was attached to a type definition

                    // Get base type names, ignoring all template classes
                    MoaiType[] baseTypes = match.Groups["baseClassName"].Captures
                        .Cast<Capture>()
                        .Select(capture => capture.Value)
                        .Where(name => !name.Contains("<"))
                        .Select(GetOrCreateType)
                        .ToArray();
                    string typeContext = string.Format("for type {0} {1}", typeName, context);
                    ParseTypeDocumentation(type, annotations, baseTypes, typeContext);
                }
            }
        }

        private void ParseTypeDocumentation(MoaiType type, Annotation[] annotations, MoaiType[] baseTypes, string context) {
            // Check that there is a single @name annotation
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                log.WarnFormat("Missing @name {0}.", context);
            } else if (nameAnnotationCount > 1) {
                log.WarnFormat("Multiple @name annotations {0}.", context);
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                log.WarnFormat("Missing @text {0}.", context);
            } else if (textAnnotationCount > 1) {
                log.WarnFormat("Multiple @text annotations {0}.", context);
            }

            // Check that all annotations are complete
            foreach (var annotation in annotations) {
                if (!annotation.IsComplete) {
                    log.WarnFormat("Incomplete {0} annotation {1}.", annotation.Command, context);
                }
            }

            // Store base types
            type.BaseTypes.AddRange(baseTypes);

            // Parse annotations
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Nothing to do. Name is already set. Just make sure the annotation is correct.
                    var nameAnnotation = (NameAnnotation) annotation;
                    if (nameAnnotation.Value != type.Name) {
                        log.WarnFormat("Inconsisten @name '{0}' {1}.", nameAnnotation.Value, context);
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
                    log.WarnFormat("Unexpected {0} annotation {1}", annotation.Command, context);
                }
            }
        }

        private static readonly Regex typeNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        private void ParseMethodDocumentation(MoaiType type, Annotation[] annotations, string context) {
            // Check that there is a single @name annotation and that it isn't a duplicate. Otherwise exit.
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                log.WarnFormat("Missing @name {0}.", context);
                return;
            }
            if (nameAnnotationCount > 1) {
                log.WarnFormat("Multiple @name annotations {0}.", context);
            }
            var nameAnnotation = annotations.OfType<NameAnnotation>().Single();
            if (type.Members.Any(member => member.Name == nameAnnotation.Value)) {
                log.WarnFormat("Multiple members with name '{0}' {1}.", nameAnnotation.Value, context);
                return;
            }

            // Check that there is a single @text annotation
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                log.WarnFormat("Missing @text {0}.", context);
            } else if (textAnnotationCount > 1) {
                log.WarnFormat("Multiple @text annotations {0}.", context);
            }

            // Check that there is at least one @out annotation
            if (!annotations.OfType<OutParameterAnnotation>().Any()) {
                log.WarnFormat("Missing @out {0}. Even for void methods, a nil annotation is expected.", context);
            }

            // Check that all annotations are complete
            foreach (var annotation in annotations) {
                if (!annotation.IsComplete) {
                    log.WarnFormat("Incomplete {0} annotation {1}.", annotation.Command, context);
                }
            }

            // Parse annotations
            // Guess if the method is static
            bool isStatic = annotations
                .OfType<InParameterAnnotation>()
                .All(param => param.Name != "self");
            var method = new MoaiMethod {
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
                    if (parameterAnnotation.Type != null && !typeNameRegex.IsMatch(parameterAnnotation.Type)) {
                        log.WarnFormat("'{0}' is no valid type {1}.", parameterAnnotation.Type, context);
                    }
                    string paramName = parameterAnnotation.Name;
                    if (annotation is InParameterAnnotation | annotation is OptionalInParameterAnnotation) {
                        // Add input parameter
                        if (currentOverload.InParameters.Any(param => param.Name == paramName)) {
                            log.WarnFormat("Multiple '{0}' params for single overload {1}.", paramName, context);
                        }
                        var inParameter = new MoaiInParameter {
                            Name = paramName,
                            Description = parameterAnnotation.Description,
                            Type = GetOrCreateType(parameterAnnotation.Type),
                            IsOptional = annotation is OptionalInParameterAnnotation
                        };
                        currentOverload.InParameters.Add(inParameter);
                    } else {
                        // Add output parameter
                        var outParameter = new MoaiOutParameter {
                            Name = paramName,
                            Type = GetOrCreateType(parameterAnnotation.Type),
                            Description = parameterAnnotation.Description
                        };
                        currentOverload.OutParameters.Add(outParameter);
                    }
                } else if (annotation is OverloadAnnotation) {
                    // Let the next parameter annotation start a new override
                    currentOverload = null;
                } else {
                    log.WarnFormat("Unexpected {0} annotation {1}", annotation.Command, context);
                }
            }
        }
    }
}