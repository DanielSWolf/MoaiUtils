using System;
using System.Collections.Generic;
using System.Linq;
using MoaiUtils.MoaiParsing.CodeGraph;

namespace MoaiUtils.MoaiParsing.Parsing {
    public static class MoaiMethodParser {
        public static void ParseMethodDocumentation(MoaiType type, Annotation[] annotations, string methodBody, MethodPosition methodPosition, MoaiTypeCollection types, WarningList warnings) {
            MoaiMethod method = CreateMethod(type, annotations, methodPosition, types, warnings);
            if (method == null) return;

            // Fill method body
            method.Body = methodBody;

            // Create compact method signature
            CreateCompactSignature(method, warnings);

            // Determine if overloads are static
            foreach (MoaiMethodOverload overload in method.Overloads) {
                var firstInParam = overload.InParameters.FirstOrDefault();
                overload.IsStatic = firstInParam == null || firstInParam.Name != "self";
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
            return nameAnnotation;
        }

    }
}