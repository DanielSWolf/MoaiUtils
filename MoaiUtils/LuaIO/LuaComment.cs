using System;
using System.Collections.Generic;
using MoaiUtils.Tools;

namespace MoaiUtils.LuaIO {

	public class LuaComment {
		public LuaComment(string text, bool blankLineBefore = false, bool blankLineAfter = false) {
			Text = text;
			BlankLineBefore = blankLineBefore;
			BlankLineAfter = blankLineAfter;
		}

		public LuaComment(IEnumerable<string> lines, bool blankLineBefore = false, bool blankLineAfter = false)
			: this(lines.Join(Environment.NewLine), blankLineBefore, blankLineAfter) {}

		public string Text { get; private set; }
		public bool BlankLineBefore { get; private set; }
		public bool BlankLineAfter { get; private set; }
	}

}