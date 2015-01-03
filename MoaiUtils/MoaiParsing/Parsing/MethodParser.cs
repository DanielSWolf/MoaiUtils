using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class MethodParser {
        private static readonly Regex methodDefinitionRegex = new Regex(@"
            # Documentation (optional)
            (?>
                /\*\*\s*
                    (?<annotation>@(\S@|\*(?!/)|[^@*])*)+
                \*/\s*
            )?
            
            # Method definition
            int\s+(?<className>[A-Za-z0-9_]+)\s*::\s*(?<methodName>[A-Za-z0-9_]+)\s*\([^)]*\)\s*\{
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public static void ParseMethodDefinitions(string code, FilePosition filePosition, TypeCollection types, WarningList warnings) {
            // Find all method definitions
            var matches = methodDefinitionRegex.Matches(code);

            // Parse method definitions
            foreach (Match match in matches) {
                string className = match.Groups["className"].Value;
                string nativeMethodName = match.Groups["methodName"].Value;
                var methodPosition = new MethodPosition(filePosition, className, nativeMethodName);

                // Parse annotations, filtering out unknown ones
                Annotation[] annotations = match.Groups["annotation"].Captures
                    .Cast<Capture>()
                    .Select(capture => Annotation.Create(capture.Value, methodPosition, warnings))
                    .ToArray();
                foreach (var unknownAnnotation in annotations.OfType<UnknownAnnotation>()) {
                    warnings.Add(methodPosition, WarningType.UnexpectedAnnotation,
                        "Unknown annotation command '{0}'.", unknownAnnotation.Command);
                }
                annotations = annotations
                    .Where(annotation => !(annotation is UnknownAnnotation))
                    .ToArray();

                // Get method body
                int openingBraceIndex = match.Index + match.Length - 1;
                int blockLength = BlockParser.GetBlockLength(code, openingBraceIndex);
                string methodBody = code.Substring(openingBraceIndex + 1, blockLength - 2);

                // Parse annotation block
                MoaiClass moaiClass = types.GetOrCreate(className, methodPosition) as MoaiClass;
                if (moaiClass != null) {
                    if (annotations.Any()) {
                        ParseMethodDocumentation(moaiClass, annotations, methodBody, methodPosition, types, warnings);
                    } else if (nativeMethodName.StartsWith("_")) {
                        warnings.Add(methodPosition, WarningType.MissingAnnotation,
                            "Missing method documentation.");
                    }
                }
            }
        }

        private static void ParseMethodDocumentation(MoaiClass moaiClass, Annotation[] annotations, string methodBody, MethodPosition methodPosition, TypeCollection types, WarningList warnings) {
            Method method = CreateMethod(moaiClass, annotations, methodPosition, types, warnings);
            if (method == null) return;

            // Fill method body
            method.Body = methodBody;

            // Create compact method signature
            CreateCompactSignature(method, warnings);

            // Determine if overloads are static
            foreach (MethodOverload overload in method.Overloads) {
                var firstInParam = overload.InParameters.FirstOrDefault();
                overload.IsStatic = firstInParam == null || firstInParam.Name != "self";
            }
        }

        private static void CreateCompactSignature(Method method, WarningList warnings) {
            try {
                method.InParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.InParameters.ToArray()));
                method.OutParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.OutParameters.ToArray()));
            } catch (Exception e) {
                warnings.Add(method.MethodPosition, WarningType.ToolLimitation,
                    "Error determining method signature. {0}", e.Message);
            }
        }

        private static ISignature GetCompactSignature(IEnumerable<CodeGraph.Parameter[]> overloads) {
            List<Parameter[]> parameterOverloads = new List<Parameter[]>();
            foreach (CodeGraph.Parameter[] overload in overloads) {
                // Input parameters may be optional. In these cases, create multiple overloads.
                for (int index = overload.Length - 1;
                    index >= 0 && overload[index] is InParameter && ((InParameter) overload[index]).IsOptional;
                    index--) {
                    parameterOverloads.Add(ConvertOverload(overload.Take(index)).ToArray());
                }
                parameterOverloads.Add(ConvertOverload(overload).ToArray());
            }

            return parameterOverloads.Any()
                ? CompactSignature.FromOverloads(parameterOverloads.ToArray())
                : new Sequence();
        }

        private static IEnumerable<Parameter> ConvertOverload(IEnumerable<CodeGraph.Parameter> overload) {
            return overload.Select(parameter => new Parameter { Name = parameter.Name, Type = parameter.Type.Name, ShowName = true });
        }

        private static Method CreateMethod(MoaiClass moaiClass, Annotation[] annotations, MethodPosition methodPosition, TypeCollection types, WarningList warnings) {
            // Get @lua annotation
            var luaNameAnnotation = GetNameAnnotation(moaiClass, annotations, methodPosition, warnings);
            if (luaNameAnnotation == null) return null;

            // Check that there is a single @text annotation
            CheckTextAnnotation(annotations, methodPosition, warnings);

            // Parse annotations
            var method = new Method {
                MethodPosition = methodPosition,
                Name = luaNameAnnotation.Value,
                OwningClass = moaiClass,
            };
            moaiClass.Members.Add(method);
            MethodOverload currentOverload = null;
            foreach (var annotation in annotations) {
                if (annotation is LuaNameAnnotation) {
                    // Nothing to do - name has already been set.
                } else if (annotation is TextAnnotation) {
                    // Set method description
                    method.Description = ((TextAnnotation) annotation).Value;
                } else if (annotation is ParameterAnnotation) {
                    if (currentOverload == null) {
                        currentOverload = new MethodOverload {OwningMethod = method};
                        method.Overloads.Add(currentOverload);
                    }
                    var parameterAnnotation = (ParameterAnnotation) annotation;
                    string paramName = parameterAnnotation.Name;
                    if (annotation is InParameterAnnotation | annotation is OptionalInParameterAnnotation) {
                        // Add input parameter
                        if (currentOverload.InParameters.Any(param => param.Name == paramName)) {
                            warnings.Add(methodPosition, WarningType.UnexpectedValue,
                                "Found multiple params with name '{0}' for single overload.", paramName);
                        }
                        var inParameter = new InParameter {
                            Name = paramName,
                            Description = parameterAnnotation.Description,
                            Type = types.GetOrCreate(parameterAnnotation.Type, methodPosition),
                            IsOptional = annotation is OptionalInParameterAnnotation
                        };
                        currentOverload.InParameters.Add(inParameter);
                    } else {
                        // Add output parameter
                        var outParameter = new OutParameter {
                            Name = paramName,
                            Type = types.GetOrCreate(parameterAnnotation.Type, methodPosition),
                            Description = parameterAnnotation.Description
                        };
                        currentOverload.OutParameters.Add(outParameter);
                    }
                } else if (annotation is OverloadAnnotation) {
                    // Let the next parameter annotation start a new override
                    currentOverload = null;
                } else {
                    warnings.Add(methodPosition, WarningType.UnexpectedAnnotation,
                        "Unexpected {0} annotation.", annotation.Command);
                }
            }
            return method;
        }

        private static void CheckTextAnnotation(Annotation[] annotations, MethodPosition methodPosition, WarningList warnings) {
            int textAnnotationCount = annotations.OfType<TextAnnotation>().Count();
            if (textAnnotationCount == 0) {
                warnings.Add(methodPosition, WarningType.MissingAnnotation, "Missing @text annotation.");
            } else if (textAnnotationCount > 1) {
                warnings.Add(methodPosition, WarningType.UnexpectedAnnotation, "Multiple @text annotations.");
            }
        }

        private static LuaNameAnnotation GetNameAnnotation(MoaiClass moaiClass, Annotation[] annotations, MethodPosition methodPosition, WarningList warnings) {
            // Check that there is a single @lua annotation and that it isn't a duplicate. Otherwise exit.
            int luaNameAnnotationCount = annotations.OfType<LuaNameAnnotation>().Count();
            if (luaNameAnnotationCount == 0) {
                warnings.Add(methodPosition, WarningType.MissingAnnotation, "Missing @lua annotation.");
                return null;
            }
            if (luaNameAnnotationCount > 1) {
                warnings.Add(methodPosition, WarningType.UnexpectedAnnotation, "Multiple @lua annotations.");
            }
            var nameAnnotation = annotations.OfType<LuaNameAnnotation>().First();
            if (moaiClass.Members.Any(member => member.Name == nameAnnotation.Value)) {
                warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "There is already a member with Lua name '{0}'.", nameAnnotation.Value);
                return null;
            }
            return nameAnnotation;
        }

    }
}