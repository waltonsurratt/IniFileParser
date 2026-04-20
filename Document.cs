using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IniParser
{
    public sealed class Document
    {
        private readonly List<IniLine> _lines = new List<IniLine>();
        private readonly Dictionary<string, Section> _sections =
            new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);

        #region Load

        public static Document Load(string path)
        {
            Encoding encoding = DetectEncoding(path);
            using var reader = new StreamReader(path, encoding, true);
            return Parse(reader);
        }

        private static Document Parse(TextReader reader)
        {
            var doc = new Document();
            string currentSection = null;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed))
                {
                    doc._lines.Add(new IniLine { Type = LineType.Blank, Raw = line });
                    continue;
                }

                if (trimmed.StartsWith(";"))
                {
                    doc._lines.Add(new IniLine
                    {
                        Type = LineType.Comment,
                        Raw = line,
                        Section = currentSection
                    });
                    continue;
                }

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);

                    doc._lines.Add(new IniLine
                    {
                        Type = LineType.Section,
                        Section = currentSection,
                        Raw = line
                    });

                    if (!doc._sections.ContainsKey(currentSection))
                        doc._sections[currentSection] =
                            new Section(currentSection, doc);

                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    string key = line.Substring(0, eq).Trim();
                    string remainder = line.Substring(eq + 1);

                    int commentIdx = remainder.IndexOf(';');
                    string value = commentIdx >= 0
                        ? remainder.Substring(0, commentIdx).Trim()
                        : remainder.Trim();

                    var iniLine = new IniLine
                    {
                        Type = LineType.KeyValue,
                        Section = currentSection,
                        Key = key,
                        Value = value,
                        Raw = line
                    };

                    doc._lines.Add(iniLine);

                    if (!doc._sections.ContainsKey(currentSection))
                        doc._sections[currentSection] =
                            new Section(currentSection, doc);

                    doc._sections[currentSection].Keys[key] =
                        new Key(key, value, iniLine);
                }
            }

            return doc;
        }

        #endregion

        #region Section API

        public Section GetSection(string sectionName)
        {
            _sections.TryGetValue(sectionName, out var section);
            return section;
        }

        public Section CreateSection(string sectionName)
        {
            if (_sections.TryGetValue(sectionName, out var existing))
                return existing;

            var section = new Section(sectionName, this);
            _sections[sectionName] = section;

            _lines.Add(new IniLine
            {
                Type = LineType.Section,
                Section = sectionName,
                Raw = $"[{sectionName}]"
            });

            return section;
        }

        public bool RemoveSection(string sectionName)
        {
            if (!_sections.Remove(sectionName))
                return false;

            _lines.RemoveAll(l => l.Section == sectionName);
            return true;
        }

        #endregion

        #region Internal Line Operations

        internal IniLine InsertKeyLine(Section section, string key, string value)
        {
            int insertIndex = _lines.Count;

            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Type == LineType.Section &&
                    _lines[i].Section.Equals(section.Name, StringComparison.OrdinalIgnoreCase))
                {
                    insertIndex = i + 1;
                    while (insertIndex < _lines.Count &&
                           _lines[insertIndex].Type != LineType.Section)
                        insertIndex++;
                    break;
                }
            }

            var line = new IniLine
            {
                Type = LineType.KeyValue,
                Section = section.Name,
                Key = key,
                Value = value,
                Raw = $"{key}={value}"
            };

            _lines.Insert(insertIndex, line);
            return line;
        }

        internal void RemoveLine(IniLine line)
        {
            _lines.Remove(line);
        }

        #endregion

        #region Save

        public void Save(string path, Encoding encoding = null)
        {
            encoding ??= Encoding.Unicode;
            using var writer = new StreamWriter(path, false, encoding);

            foreach (var line in _lines)
                writer.WriteLine(line.Raw);
        }

        #endregion

        #region Merge

        public void Merge(Document other)
        {
            foreach (var section in other._sections.Values)
            {
                var target = GetSection(section.Name) ?? CreateSection(section.Name);

                foreach (var key in section.Keys.Values)
                {
                    target.CreateKey(key.Name).SetValue(key.Value);
                }
            }
        }

        #endregion

        #region Compare

        public IEnumerable<string> Compare(Document other)
        {
            foreach (var sec in _sections)
            {
                if (!other._sections.TryGetValue(sec.Key, out var otherSec))
                {
                    yield return $"Missing section: [{sec.Key}]";
                    continue;
                }

                foreach (var key in sec.Value.Keys)
                {
                    if (!otherSec.Keys.TryGetValue(key.Key, out var otherKey))
                    {
                        yield return $"Missing key: [{sec.Key}] {key.Key}";
                    }
                    else if (otherKey.Value != key.Value.Value)
                    {
                        yield return $"Value mismatch: [{sec.Key}] {key.Key}";
                    }
                }
            }
        }

        #endregion

        #region Encoding

        private static Encoding DetectEncoding(string path)
        {
            using var stream = File.OpenRead(path);
            byte[] bom = new byte[4];
            stream.Read(bom, 0, 4);

            if (bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;

            if (bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            return Encoding.Default; // ANSI
        }

        #endregion
    }
}