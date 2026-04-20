namespace IniParser
{
    internal sealed class IniLine
    {
        public LineType Type;
        public string Raw;      // Original text of the line (preserved verbatim)
        public string Section;  // Section this line belongs to
        public string Key;      // Key name (KeyValue only)
        public string Value;    // Value without inline comment
    }
}