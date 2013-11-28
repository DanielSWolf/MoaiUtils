using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CreateCodeCompletionDatabase.Graph;
using log4net;
using Tools;

namespace CreateCodeCompletionDatabase {
    public class MoaiCodeParser {
        private static readonly ILog log = LogManager.GetLogger(typeof(MoaiCodeParser));

        private Dictionary<string, MoaiType> typesByName = new Dictionary<string, MoaiType>();

        public void Parse(DirectoryInfo moaiSourceDirectory) {
            // Check that the input directory looks like the Moai src directory
            if (!moaiSourceDirectory.GetDirectoryInfo("moai-core").Exists) {
                throw new ApplicationException(string.Format("Path '{0}' does not appear to be the 'src' directory of a Moai source copy.", moaiSourceDirectory));
            }

            // Parse Moai types and store them by type name
            typesByName = new Dictionary<string, MoaiType>();
            ParseMoaiCodeFiles(moaiSourceDirectory);

            // MOAILuaObject is not documented, probably because it would mess up
            // the Doxygen-generated documentation. Use dummy code instead.
            ParseMoaiCode(MoaiLuaObject.DummyCode, "in MoaiLuaObject dummy code");

            // Make sure every class directly or indirectly inherits from MOAILuaObject
            MoaiType moaiLuaObjectType = GetOrCreateType("MOAILuaObject");
            foreach (MoaiType type in typesByName.Values) {
                if (!(type.AncestorTypes.Contains(moaiLuaObjectType)) && type != moaiLuaObjectType) {
                    type.BaseTypes.Add(moaiLuaObjectType);
                }
            }
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
            // Check that there is a single @name annotation. Otherwise exit.
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                log.WarnFormat("Missing @name {0}.", context);
                return;
            }
            if (nameAnnotationCount > 1) {
                log.WarnFormat("Multiple @name annotations {0}.", context);
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
            var method = new MoaiMethod { OwningType = type, IsStatic = isStatic };
            type.Members.Add(method);
            MethodOverride currentOverride = null;
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
                    // Set method name
                    var nameAnnotation = (NameAnnotation) annotation;
                    method.Name = nameAnnotation.Value;
                } else if (annotation is TextAnnotation) {
                    // Set method description
                    method.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is ParameterAnnotation) {
                    if (currentOverride == null) {
                        currentOverride = new MethodOverride { OwningMethod = method };
                        method.Overrides.Add(currentOverride);
                    }
                    var parameterAnnotation = (ParameterAnnotation) annotation;
                    if (parameterAnnotation.Type != null && !typeNameRegex.IsMatch(parameterAnnotation.Type)) {
                        log.WarnFormat("'{0}' is no valid type {1}.", parameterAnnotation.Type, context);
                    }
                    if (annotation is InParameterAnnotation | annotation is OptionalInParameterAnnotation) {
                        // Add input parameter
                        var parameter = new MoaiParameter {
                            Name = parameterAnnotation.Name,
                            Description = parameterAnnotation.Description,
                            Type = GetOrCreateType(parameterAnnotation.Type),
                            IsOptional = annotation is OptionalInParameterAnnotation
                        };
                        currentOverride.Parameters.Add(parameter);
                    } else {
                        // A nil return value is helpful to show the documentation is complete,
                        // but it needs no representation.
                        if (parameterAnnotation.Type == "nil") continue;

                        // Add return value
                        var returnValue = new MoaiReturnValue {
                            Type = GetOrCreateType(parameterAnnotation.Type),
                            // Return values have no name. Merge name into description.
                            Description = (parameterAnnotation.Description != null)
                                ? string.Format("{0}: {1}", parameterAnnotation.Name, parameterAnnotation.Description)
                                : parameterAnnotation.Name
                        };
                        currentOverride.ReturnValues.Add(returnValue);
                    }
                } else if (annotation is OverloadAnnotation) {
                    // Let the next parameter annotation start a new override
                    currentOverride = null;
                } else {
                    log.WarnFormat("Unexpected {0} annotation {1}", annotation.Command, context);
                }
            }
        }
    }
}