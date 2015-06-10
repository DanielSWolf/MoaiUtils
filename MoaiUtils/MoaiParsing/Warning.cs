using System.Collections.Generic;

namespace MoaiUtils.MoaiParsing {

	public class WarningList : List<Warning> {
		public void Add(FilePosition position, WarningType type, string message) {
			Add(new Warning(position, type, message));
		}

		public void Add(FilePosition position, WarningType type, string messageFormat, params object[] args) {
			Add(position, type, string.Format(messageFormat, args));
		}
	}

	public class Warning {
		public Warning(FilePosition position, WarningType type, string message) {
			Position = position;
			Type = type;
			Message = message;
		}

		public FilePosition Position { get; private set; }
		public WarningType Type { get; private set; }
		public string Message { get; private set; }
	}

	public enum WarningType {
		// A required annotation is missing
		MissingAnnotation,

		// This annotation is not allowed in this place or is altogether unknown
		UnexpectedAnnotation,

		// An annotation is missing a required value
		IncompleteAnnotation,

		// An annotation value appears to be wrong
		UnexpectedValue,

		// Documentation looks suspicious without necessarily being wrong
		HeuristicWarning,

		// There is an error with the way a Lua methd is registered
		IncorrectMethodRegistration,

		// MoaiUtils couldn't handle a particular case
		ToolLimitation
	}

}