namespace IniParser
{
    public sealed class Key
    {
        public string Name { get; }
        public string Value { get; private set; }

        internal IniLine Line;

        internal Key(string name, string value, IniLine line)
        {
            Name = name;
            Value = value;
            Line = line;
        }

        public Key SetValue(string newValue)
        {
            int commentIdx = Line.Raw.IndexOf(';');
            string comment = commentIdx >= 0
                ? Line.Raw.Substring(commentIdx)
                : string.Empty;

            Line.Value = newValue;
            Line.Raw = $"{Name}={newValue}{(comment.Length > 0 ? " " : "")}{comment}";
            Value = newValue;

            return this;
        }
    }
}