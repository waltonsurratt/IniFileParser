using System;
using System.Collections.Generic;

namespace IniParser
{
    public sealed class Section
    {
        public string Name { get; }

        internal readonly Dictionary<string, Key> Keys =
            new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);

        internal readonly Document Owner;

        internal Section(string name, Document owner)
        {
            Name = name;
            Owner = owner;
        }

        public Key GetKey(string keyName)
        {
            Keys.TryGetValue(keyName, out var key);
            return key;
        }

        public Key CreateKey(string keyName, string value = "")
        {
            if (Keys.TryGetValue(keyName, out var existing))
                return existing;

            IniLine line = Owner.InsertKeyLine(this, keyName, value);
            var key = new Key(keyName, value, line);

            Keys[keyName] = key;
            return key;
        }

        public bool RemoveKey(string keyName)
        {
            if (!Keys.TryGetValue(keyName, out var key))
                return false;

            Owner.RemoveLine(key.Line);
            Keys.Remove(keyName);
            return true;
        }
    }
}