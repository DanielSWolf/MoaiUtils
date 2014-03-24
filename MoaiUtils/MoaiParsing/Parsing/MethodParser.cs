using System;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;
using Type = MoaiUtils.MoaiParsing.CodeGraph.Type;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class MethodParser {
        public static void ParseMethodDocumentation(Type type, Annotation[] annotations, string methodBody, MethodPosition methodPosition, TypeCollection types, WarningList warnings) {
            Method method = CreateMethod(type, annotations, methodPosition, types, warnings);
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

        private static Method CreateMethod(Type type, Annotation[] annotations, MethodPosition methodPosition, TypeCollection types, WarningList warnings) {
            // Get @name annotation
            var nameAnnotation = GetNameAnnotation(type, annotations, methodPosition, warnings);
            if (nameAnnotation == null) return null;

            // Check that there is a single @text annotation
            CheckTextAnnotation(annotations, methodPosition, warnings);

            // Parse annotations
            var method = new Method {
                MethodPosition = methodPosition,
                Name = nameAnnotation.Value,
                OwningType = type,
            };
            type.Members.Add(method);
            MethodOverload currentOverload = null;
            foreach (var annotation in annotations) {
                if (annotation is NameAnnotation) {
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

        private static NameAnnotation GetNameAnnotation(Type type, Annotation[] annotations, MethodPosition methodPosition, WarningList warnings) {
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
            return nameAnnotation;
        }

    }
}