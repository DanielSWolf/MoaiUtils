using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.MoaiParsing.CodeGraph.Types;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatAllOverloadsAreDocumented : CheckBase {

        private static readonly Regex paramAccessRegex = new Regex(
            @"state\s*\.\s*(GetLuaObject|GetValue)\s*<\s*(?<type>[A-Za-z0-9_*]+)\s*>\s*\(\s*(?<index>[0-9]+)\s*,",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public override void Run() {
            foreach (Method method in Methods) {
                // Make sure the method has at least one overload
                if (!method.Overloads.Any()) {
                    Warnings.Add(method.MethodPosition, WarningType.MissingAnnotation,
                        "Method documentation is missing method signature.");
                }

                // Analyze body to find undocumented overloads
                var matches = paramAccessRegex.Matches(method.Body);
                foreach (Match match in matches) {
                    // Find the Lua name for the param name
                    string paramTypeName = match.Groups["type"].Value;
                    IType paramType = Types.Find(paramTypeName, MatchMode.FindSynonyms, t => t.IsConfirmed);
                    if (paramType != null) paramTypeName = paramType.Name;

                    int index = Int32.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);
                    bool isDocumented = method.Overloads.Any(overload => {
                        if (overload.InParameters.Count < index) return false;
                        var param = overload.InParameters[index - 1];
                        return param.Type.Name == paramTypeName;
                    });
                    if (!isDocumented) {
                        Warnings.Add(method.MethodPosition, WarningType.MissingAnnotation,
                            "Missing documentation for parameter #{0} of type {1}.", index, paramTypeName);
                    }
                }
            }
        }

    }
}