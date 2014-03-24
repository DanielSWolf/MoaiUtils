using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.MoaiParsing.CodeGraph;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing.Checks {
    public class CheckThatMethodsAreRegisteredCorrectly : CheckBase {

        private static readonly Regex registrationMethodRegex = new Regex(@"
            void\s+(?<class>[A-Za-z0-9_]+)\s*::\s*(?<registrationMethod>RegisterLuaFuncs|RegisterLuaClass)\s*
            \(\s*MOAILuaState[^}]+?
            luaL_Reg.+?=\s*\{\s*
            (?<registrations>[\s\S]*?)
            \};",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private static readonly Regex registrationRegex = new Regex(
            @"\{\s*""(?<luaName>[A-Za-z0-9_]+)""\s*,\s*(?<nativeName>[A-Za-z0-9_]+)\s*\}",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public override void Run() {
            // Get all method registrations
            Dictionary<string, List<MethodRegistration>> registrationsByClass = ParseMethodRegistrations()
                .GroupBy(registration => registration.ClassName)
                .ToDictionary(classGroup => classGroup.Key, classGroup => classGroup.ToList());

            // Check that all methods are registered as expected
            foreach (Method method in Methods) {
                CheckMethodRegistration(method, registrationsByClass);
            }
        }

        private void CheckMethodRegistration(Method method, Dictionary<string, List<MethodRegistration>> registrationsByType) {
            var methodRegistrations = FindMethodRegistrations(method, registrationsByType).ToArray();
            string fullMethodName = string.Format("{0}::{1}()", method.OwningClass.Name, method.MethodPosition.NativeMethodName);

            bool bodyIsEmpty = string.IsNullOrWhiteSpace(method.Body);
            if (bodyIsEmpty) {
                // Empty methods are only stubs for Doxygen
                if (methodRegistrations.Any()) {
                    Warnings.Add(methodRegistrations.First().MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} appears to be an empty Doxygen stub, but is registered.", fullMethodName);
                }
                return;
            }

            // Make sure the method is registered
            if (!methodRegistrations.Any()) {
                Warnings.Add(method.MethodPosition, WarningType.IncorrectMethodRegistration,
                    "Method {0} is never registered.", fullMethodName);
                return;
            }

            var staticRegistrations = methodRegistrations
                .Where(registration => registration.RegisteredAsStatic)
                .ToArray();
            var instanceRegistrations = methodRegistrations
                .Where(registration => !registration.RegisteredAsStatic)
                .ToArray();

            if (method.Overloads.Any(overload => overload.IsStatic)) {
                // Make sure the method is registered as static
                if (!staticRegistrations.Any()) {
                    Warnings.Add(method.MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} has static overloads but is not registered in RegisterLuaClass().", fullMethodName);
                }

                // Make sure the method is registered as static only once
                if (staticRegistrations.Count() > 1) {
                    Warnings.Add(staticRegistrations.Last().MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} is registered more than once in RegisterLuaClass().", fullMethodName);
                }
            } else if (method.Overloads.Any()) {
                // Check that instance-only methods are not registered as static
                if (staticRegistrations.Any()) {
                    Warnings.Add(staticRegistrations.First().MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} has only non-static overloads but is registered in RegisterLuaClass().", fullMethodName);
                }
            }

            if (method.Overloads.Any(overload => !overload.IsStatic)) {
                // Make sure the method is registered as non-static
                if (!instanceRegistrations.Any()) {
                    Warnings.Add(method.MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} has non-static overloads but is not registered in RegisterLuaFuncs().", fullMethodName);
                }

                // Make sure the method is registered as non-static only once
                if (instanceRegistrations.Count() > 1) {
                    Warnings.Add(instanceRegistrations.Last().MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} is registered more than once in RegisterLuaFuncs().", fullMethodName);
                }
            } else if (method.Overloads.Any()) {
                // Check that static-only methods are not registered as non-static
                if (instanceRegistrations.Any()) {
                    Warnings.Add(instanceRegistrations.First().MethodPosition, WarningType.IncorrectMethodRegistration,
                        "Method {0} has only static overloads but is registered in RegisterLuaFuncs().", fullMethodName);
                }
            }

            // Make sure the Lua name matches
            foreach (MethodRegistration registration in methodRegistrations) {
                if (registration.LuaMethodName != method.Name) {
                    Warnings.Add(registration.MethodPosition, WarningType.UnexpectedValue,
                        "Method has @name annotation '{0}' but is registered as '{1}'.", method.Name, registration.LuaMethodName);
                }
            }
        }

        private IEnumerable<MethodRegistration> FindMethodRegistrations(Method method, Dictionary<string, List<MethodRegistration>> registrationsByClass) {
            List<MethodRegistration> methodRegistrations;
            if (!registrationsByClass.TryGetValue(method.OwningClass.Name, out methodRegistrations)) {
                return Enumerable.Empty<MethodRegistration>();
            }

            return methodRegistrations
                .Where(registration => registration.NativeMethodName == method.MethodPosition.NativeMethodName);
        }

        private IEnumerable<MethodRegistration> ParseMethodRegistrations() {
            var files = MoaiSrcDirectory.GetFilesRecursively(".h", ".cpp");
            foreach (FileInfo fileInfo in files) {
                FilePosition filePosition = new FilePosition(fileInfo);
                string code = fileInfo.ReadAllText();
                var registrationMethodMatches = registrationMethodRegex.Matches(code);
                foreach (Match registrationMethodMatch in registrationMethodMatches) {
                    string className = registrationMethodMatch.Groups["class"].Value;
                    string registrationMethodName = registrationMethodMatch.Groups["registrationMethod"].Value;
                    string registrations = registrationMethodMatch.Groups["registrations"].Value;
                    var registrationMatches = registrationRegex.Matches(registrations);
                    foreach (Match registrationMatch in registrationMatches) {
                        string luaMethodName = registrationMatch.Groups["luaName"].Value;
                        string nativeMethodName = registrationMatch.Groups["nativeName"].Value;
                        yield return new MethodRegistration(
                            className, luaMethodName, nativeMethodName, registrationMethodName, filePosition);
                    }
                }
            }
        }

        private class MethodRegistration {
            public MethodRegistration(string className, string luaMethodName, string nativeMethodName, string registrationMethodName, FilePosition filePosition) {
                ClassName = className;
                LuaMethodName = luaMethodName;
                NativeMethodName = nativeMethodName;
                RegistrationMethodName = registrationMethodName;
                MethodPosition = new MethodPosition(filePosition, className, registrationMethodName);
            }

            public string ClassName { get; private set; }
            public string LuaMethodName { get; private set; }
            public string NativeMethodName { get; private set; }
            public string RegistrationMethodName { get; private set; }
            public MethodPosition MethodPosition { get; private set; }

            public bool RegisteredAsStatic {
                get { return RegistrationMethodName == "RegisterLuaClass"; }
            }
        }

    }
}