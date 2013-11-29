using System;
using System.Linq;
using System.Text.RegularExpressions;
using Tools;

namespace CreateApiDescription {
    public abstract class Annotation {
        private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        protected Annotation(string[] elements) {
            CheckElements(elements);
            Elements = elements;
        }

        public static Annotation Create(string text) {
            if (text == null) throw new ArgumentNullException("text");
            if (!text.StartsWith("@")) throw new ArgumentException("Annotation must start with an '@' character.");

            var elements = whitespaceRegex.Split(text.Trim()).ToArray();
            CheckElements(elements);
            string command = elements[0];
            switch (command) {
                case "@name":
                    return new NameAnnotation(elements);
                case "@text":
                    return new TextAnnotation(elements);
                case "@const":
                    return new ConstantAnnotation(elements);
                case "@flag":
                    return new FlagAnnotation(elements);
                case "@attr":
                    return new AttributeAnnotation(elements);
                case "@in":
                    return new InParameterAnnotation(elements);
                case "@opt":
                    return new OptionalInParameterAnnotation(elements);
                case "@out":
                    return new OutParameterAnnotation(elements);
                case "@overload":
                    return new OverloadAnnotation(elements);
                default:
                    return new UnknownAnnotation(elements);
            }
        }

        public string[] Elements { get; set; }

        public string Command {
            get { return Elements[0]; }
        }

        public abstract bool IsComplete { get; }

        protected string GetElementAt(int index) {
            if (index >= Elements.Length) return null;
            return Elements[index];
        }

        protected string GetStringStartingAt(int index) {
            if (index >= Elements.Length) return null;
            string htmlString = Elements.Skip(index).Join(" ");
            return ToPlainText(htmlString);
        }

        private static void CheckElements(string[] elements) {
            if (elements == null) throw new ArgumentNullException("elements");
            if (elements.Length < 1) throw new ArgumentException("An annotation needs at least one element, its command.");
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
        public NameAnnotation(string[] elements) : base(elements) { }

        public string Value {
            get { return GetElementAt(1); }
        }

        public override bool IsComplete {
            get { return Value != null; }
        }
    }

    public class TextAnnotation : Annotation {
        public TextAnnotation(string[] elements) : base(elements) { }

        public string Value {
            get { return GetStringStartingAt(1); }
        }

        public override bool IsComplete {
            get { return Value != null; }
        }
    }

    public abstract class FieldAnnotation : Annotation {
        protected FieldAnnotation(string[] elements) : base(elements) { }

        public string Name {
            get { return GetElementAt(1); }
        }

        public string Description {
            get { return GetStringStartingAt(2); }
        }

        public override bool IsComplete {
            get { return Description != null; }
        }
    }

    public class ConstantAnnotation : FieldAnnotation {
        public ConstantAnnotation(string[] elements) : base(elements) { }
    }

    public class FlagAnnotation : FieldAnnotation {
        public FlagAnnotation(string[] elements) : base(elements) { }
    }

    public class AttributeAnnotation : FieldAnnotation {
        public AttributeAnnotation(string[] elements) : base(elements) { }
    }

    public abstract class ParameterAnnotation : Annotation {
        protected ParameterAnnotation(string[] elements) : base(elements) { }

        public string Type {
            get { return GetElementAt(1); }
        }

        public string Name {
            get { return GetElementAt(2); }
        }

        public string Description {
            get { return GetStringStartingAt(3); }
        }

        public override bool IsComplete {
            get {
                // Let's not insist on a description for well-named parameters.
                return Name != null;
            }
        }
    }

    public class InParameterAnnotation : ParameterAnnotation {
        public InParameterAnnotation(string[] elements) : base(elements) { }
    }

    public class OptionalInParameterAnnotation : ParameterAnnotation {
        public OptionalInParameterAnnotation(string[] elements) : base(elements) { }
    }

    public class OutParameterAnnotation : ParameterAnnotation {
        public OutParameterAnnotation(string[] elements) : base(elements) { }

        public override bool IsComplete {
            get {
                // The return type often suffices.
                return Type != null;
            }
        }
    }

    public class OverloadAnnotation : Annotation {
        public OverloadAnnotation(string[] elements) : base(elements) {}

        public override bool IsComplete {
            get { return true; }
        }
    }

    public class UnknownAnnotation : Annotation {
        public UnknownAnnotation(string[] elements) : base(elements) { }

        public override bool IsComplete {
            get { return true; }
        }
    }
}