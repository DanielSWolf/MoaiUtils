namespace LuaIO {
    public class LuaComment {
        public LuaComment(string text, bool blankLineBefore = false, bool blankLineAfter = false) {
            Text = text;
            BlankLineBefore = blankLineBefore;
            BlankLineAfter = blankLineAfter;
        }

        public string Text { get; private set; }
        public bool BlankLineBefore { get; private set; }
        public bool BlankLineAfter { get; private set; }
    }
}