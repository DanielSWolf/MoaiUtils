using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MoaiUtils.Tools;

namespace MoaiUtils.MoaiParsing {
    public abstract class Annotation {
        private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex wordWithWhitespaceRegex = new Regex(@"(?<word>\S+)(?<whitespace>\s*)", RegexOptions.Compiled);

        protected Annotation(string text, FilePosition filePosition) {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("text is empty.");

            IEnumerable<Match> matches = wordWithWhitespaceRegex.Matches(text.Trim()).Cast<Match>();
            Elements = matches
                .Select(match => match.Groups["word"].Value)
                .ToArray();
            WhitespaceAfterElements = matches
                .Select(match => match.Groups["whitespace"].Value)
                .ToArray();

            FilePosition = filePosition;
        }

        public static Annotation Create(string text, FilePosition filePosition, WarningList warnings) {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("text is empty.");

            Match match = wordWithWhitespaceRegex.Match(text.Trim());
            string command = match.Groups["word"].Value;
            switch (command) {
                case "@name":
                    return new NameAnnotation(text, filePosition, warnings);
                case "@text":
                    return new TextAnnotation(text, filePosition, warnings);
                case "@const":
                    return new ConstantAnnotation(text, filePosition, warnings);
                case "@flag":
                    return new FlagAnnotation(text, filePosition, warnings);
                case "@attr":
                    return new AttributeAnnotation(text, filePosition, warnings);
                case "@in":
                    return new InParameterAnnotation(text, filePosition, warnings);
                case "@opt":
                    return new OptionalInParameterAnnotation(text, filePosition, warnings);
                case "@out":
                    return new OutParameterAnnotation(text, filePosition, warnings);
                case "@overload":
                    return new OverloadAnnotation(text, filePosition);
                default:
                    return new UnknownAnnotation(text, filePosition);
            }
        }

        public string[] Elements { get; private set; }
        public string[] WhitespaceAfterElements { get; private set; }
        public FilePosition FilePosition { get; private set; }

        public string Command {
            get { return Elements[0]; }
        }

        protected string GetElementAt(int index) {
            if (index >= Elements.Length) return null;
            return Elements[index];
        }

        protected string GetStringStartingAt(int index) {
            if (index >= Elements.Length) return null;
            string htmlString = Elements.Skip(index).Join(" ");
            return ToPlainText(htmlString);
        }

        private static readonly Regex paragraphEntityRegex = new Regex(@"<\s*(p|/\s*p|ul|/\s*ul|/\s*li)\s*>", RegexOptions.Compiled);
        private static readonly Regex bulletEntityRegex = new Regex(@"<\s*li\s*>", RegexOptions.Compiled);
        private static readonly Regex newlinesRegex = new Regex("\\s*\n\\s*", RegexOptions.Compiled);

        /// <summary>
        /// Moai's documentation is in HTML, but uses only a handful of tags.
        /// This method converts such a pesudo-HTML string to plain text.
        /// </summary>
        private static string ToPlainText(string s) {
            // Replace all whitespace with a single space
            s = whitespaceRegex.Replace(s, " ");

            // Replace all paragraph-like entities with a single linebreak
            s = paragraphEntityRegex.Replace(s, "\n");

            // Replace list items with '-' bullets
            s = bulletEntityRegex.Replace(s, "\n- ");

            // Collapse multiple newlines into a single environment-specific one
            s = newlinesRegex.Replace(s, Environment.NewLine);

            return s.Trim();
        }
    }

    public class NameAnnotation : Annotation {
        public NameAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition) {
            if (Value == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation is missing its value.", Command);
            }
        }

        public string Value {
            get { return GetElementAt(1); }
        }
    }

    public class TextAnnotation : Annotation {
        public TextAnnotation(string text, FilePosition filePosition, WarningList warnings) : base(text, filePosition) {
            if (Value == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation is missing its value.", Command);
            }
        }

        public string Value {
            get { return GetStringStartingAt(1); }
        }
    }

    public abstract class FieldAnnotation : Annotation {
        protected FieldAnnotation(string text, FilePosition filePosition, WarningList warnings) : base(text, filePosition) {
            if (Name == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation is missing its name (1st word).", Command);
            }
            if (Description == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation '{1}' is missing its description (2nd word+).", Command, Name);
            }
        }

        public string Name {
            get { return GetElementAt(1); }
        }

        public string Description {
            get { return GetStringStartingAt(2); }
        }
    }

    public class ConstantAnnotation : FieldAnnotation {
        public ConstantAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) { }
    }

    public class FlagAnnotation : FieldAnnotation {
        public FlagAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) { }
    }

    public class AttributeAnnotation : FieldAnnotation {
        public AttributeAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) { }
    }

    public abstract class ParameterAnnotation : Annotation {
        protected ParameterAnnotation(string text, FilePosition filePosition, WarningList warnings) : base(text, filePosition) {
            if (Type == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation is missing its type (1st word).", Command);
            }
            // Not all parameter annotations require a type or name.
            // Let the derived classes decide.

            if (Description != null && WhitespaceAfterElements[2] == " ") {
                // There is only a single space before the description
                warnings.Add(filePosition, WarningType.HeuristicWarning,
                    "{0} annotation has only a single space between its name ('{1}') and its description ('{2}'). This often indicates that the description is not self-contained.",
                    Command, Name, Description.GetExcerpt());
            }
        }

        public string Type {
            get { return GetElementAt(1); }
        }

        public string Name {
            get { return GetElementAt(2); }
        }

        public string Description {
            get { return GetStringStartingAt(3); }
        }
    }

    public class InParameterAnnotation : ParameterAnnotation {
        public InParameterAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) {
            if (Name == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation with type '{1}' is missing its name (2nd word).", Command, Type);
            }
            // Let's not insist on a description for well-named parameters.
        }
    }

    public class OptionalInParameterAnnotation : ParameterAnnotation {
        public OptionalInParameterAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) {
            if (Name == null) {
                warnings.Add(filePosition, WarningType.IncompleteAnnotation,
                    "{0} annotation with type '{1}' is missing its name (2nd word).", Command, Type);
            }
            // Let's not insist on a description for well-named parameters.
        }
    }

    public class OutParameterAnnotation : ParameterAnnotation {
        public OutParameterAnnotation(string text, FilePosition filePosition, WarningList warnings)
            : base(text, filePosition, warnings) { }
    }

    public class OverloadAnnotation : Annotation {
        public OverloadAnnotation(string text, FilePosition filePosition) : base(text, filePosition) { }
    }

    public class UnknownAnnotation : Annotation {
        public UnknownAnnotation(string text, FilePosition filePosition) : base(text, filePosition) { }
    }
}