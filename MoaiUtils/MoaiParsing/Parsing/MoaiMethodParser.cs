using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class MoaiMethodParser {

        public static void ParseMethodDocumentation(MoaiType type, Annotation[] annotations, string methodBody, MethodPosition methodPosition, MoaiTypeCollection types, WarningList warnings) {
            MoaiMethod method = CreateMethod(type, annotations, methodPosition, types, warnings);
            if (method == null) return;

            // Make sure the method has at least one overload
            if (!method.Overloads.Any()) {
                warnings.Add(method.MethodPosition, WarningType.MissingAnnotation,
                    "No documentation with method signature found.");
            }

            // Create compact method signature
            CreateCompactSignature(method, warnings);

            // Determine if overloads are static
            foreach (MoaiMethodOverload overload in method.Overloads) {
                var firstInParam = overload.InParameters.FirstOrDefault();
                overload.IsStatic = firstInParam == null || firstInParam.Name != "self";
            }

            // Check that there is at least one @out annotation per overload
            foreach (MoaiMethodOverload overload in method.Overloads) {
                if (!overload.OutParameters.Any()) {
                    warnings.Add(methodPosition, WarningType.MissingAnnotation,
                        "Missing @out annotation. Even for void methods, an @out annotation with type nil is expected.");
                }
            }

            // Make sure that no required parameter follows an optional one
            foreach (MoaiMethodOverload overload in method.Overloads) {
                if (overload.InParameters.Pairwise().Any(pair => pair.Item1.IsOptional && !pair.Item2.IsOptional)) {
                    warnings.Add(methodPosition, WarningType.UnexpectedAnnotation,
                        "Required params (@in) are not allowed after optional params (@opt).");
                }
            }

            // Check 'self' params
            foreach (var overload in method.Overloads) {
                if (overload.InParameters.Skip(1).Any(param => param.Name == "self")) {
                    warnings.Add(methodPosition, WarningType.UnexpectedValue,
                        "'self' param must be at index 1.");
                }
                var firstInParam = overload.InParameters.FirstOrDefault();
                if (firstInParam != null && firstInParam.Name == "self") {
                    if (firstInParam.Type != method.OwningType) {
                        warnings.Add(methodPosition, WarningType.UnexpectedValue,
                            "'self' param is of type {0}. Expected {1}.",
                            firstInParam.Type != null ? firstInParam.Type.Name : "unknown", method.OwningType.Name);
                    }
                }
            }

            // Analyze body to find undocumented overloads
            var matches = paramAccessRegex.Matches(methodBody);
            foreach (Match match in matches) {
                // Find the Lua name for the param name
                string paramTypeName = match.Groups["type"].Value;
                MoaiType paramType = types.Find(paramTypeName, MatchMode.FindSynonyms, t => t.IsDocumented | t.IsPrimitive);
                if (paramType != null) paramTypeName = paramType.Name;

                int index = int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
                bool isDocumented = method.Overloads.Any(overload => {
                    if (overload.InParameters.Count < index) return false;
                    var param = overload.InParameters[index - 1];
                    return param.Type.Name == paramTypeName;
                });
                if (!isDocumented) {
                    warnings.Add(methodPosition, WarningType.MissingAnnotation,
                        "Missing documentation for parameter #{0} of type {1}.", index, paramTypeName);
                }
            }
        }

        private static void CreateCompactSignature(MoaiMethod method, WarningList warnings) {
            try {
                method.InParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.InParameters.ToArray()));
                method.OutParameterSignature = GetCompactSignature(method.Overloads.Select(overload => overload.OutParameters.ToArray()));
            } catch (Exception e) {
                warnings.Add(method.MethodPosition, WarningType.ToolLimitation,
                    "Error determining method signature. {0}", e.Message);
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

            return parameterOverloads.Any()
                ? CompactSignature.FromOverloads(parameterOverloads.ToArray())
                : new Sequence();
        }

        private static IEnumerable<Parameter> ConvertOverload(IEnumerable<MoaiParameter> overload) {
            return overload.Select(parameter => new Parameter { Name = parameter.Name, Type = parameter.Type.Name, ShowName = true });
        }

        private static MoaiMethod CreateMethod(MoaiType type, Annotation[] annotations, MethodPosition methodPosition, MoaiTypeCollection types, WarningList warnings) {
            // Get @name annotation
            var nameAnnotation = GetNameAnnotation(type, annotations, methodPosition, warnings);
            if (nameAnnotation == null) return null;

            // Check that there is a single @text annotation
            CheckTextAnnotation(annotations, methodPosition, warnings);

            // Parse annotations
            var method = new MoaiMethod {
                MethodPosition = methodPosition,
                Name = nameAnnotation.Value,
                OwningType = type,
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
                        currentOverload = new MoaiMethodOverload {OwningMethod = method};
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
                        var inParameter = new MoaiInParameter {
                            Name = paramName,
                            Description = parameterAnnotation.Description,
                            Type = types.GetOrCreate(parameterAnnotation.Type, methodPosition),
                            IsOptional = annotation is OptionalInParameterAnnotation
                        };
                        currentOverload.InParameters.Add(inParameter);
                    } else {
                        // Add output parameter
                        var outParameter = new MoaiOutParameter {
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

        private static NameAnnotation GetNameAnnotation(MoaiType type, Annotation[] annotations, MethodPosition methodPosition, WarningList warnings) {
            // Check that there is a single @name annotation and that it isn't a duplicate. Otherwise exit.
            int nameAnnotationCount = annotations.OfType<NameAnnotation>().Count();
            if (nameAnnotationCount == 0) {
                warnings.Add(methodPosition, WarningType.MissingAnnotation, "Missing @name annotation.");
                return null;
            }
            if (nameAnnotationCount > 1) {
                warnings.Add(methodPosition, WarningType.UnexpectedAnnotation, "Multiple @name annotations.");
            }
            var nameAnnotation = annotations.OfType<NameAnnotation>().Single();
            if (type.Members.Any(member => member.Name == nameAnnotation.Value)) {
                warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "There is already a member with name '{0}'.", nameAnnotation.Value);
                return null;
            }

            // Check that @name annotation sticks to convention
            if (!methodPosition.NativeMethodName.StartsWith("_")) {
                warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "Unexpected C++ method name '{0}'. By convention, the name of a Lua method implementation shold start with an underscore.",
                    methodPosition.NativeMethodName);
            }
            string expectedName = methodPosition.NativeMethodName.Substring(1);
            if (nameAnnotation.Value != expectedName) {
                warnings.Add(methodPosition, WarningType.UnexpectedValue,
                    "@name annotation has unexpected value '{0}'. By convention expected '{1}'.",
                    nameAnnotation.Value, expectedName);
            }
            return nameAnnotation;
        }

        private static readonly Regex paramAccessRegex = new Regex(
            @"state\s*\.\s*(GetLuaObject|GetValue)\s*<\s*(?<type>[A-Za-z0-9_*]+)\s*>\s*\(\s*(?<index>[0-9]+)\s*,",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    }
}