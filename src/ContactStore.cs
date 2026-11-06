using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyIP
{
    public sealed class ContactRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Note { get; set; }
        public string UpdatedAt { get; set; }

        public ContactRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = "";
            IpAddress = "";
            Note = "";
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// Portable local database next to the EXE (contacts.db, JSON).
    /// No third-party DLL — works on Win7/Win10 with .NET Framework 4.0+.
    /// </summary>
    public sealed class ContactStore
    {
        private readonly string _dbPath;
        private List<ContactRecord> _items = new List<ContactRecord>();

        public ContactStore(string dbPath)
        {
            _dbPath = dbPath;
            Load();
        }

        public IList<ContactRecord> Items
        {
            get { return _items.AsReadOnly(); }
        }

        public string DbPath
        {
            get { return _dbPath; }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_dbPath))
                {
                    _items = new List<ContactRecord>();
                    Save();
                    return;
                }

                string json = File.ReadAllText(_dbPath, Encoding.UTF8);
                _items = DeserializeList(json);
            }
            catch
            {
                _items = new List<ContactRecord>();
            }
        }

        public void Save()
        {
            string dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_dbPath, SerializeList(_items), Encoding.UTF8);
        }

        public ContactRecord Add(string name, string ip, string note)
        {
            ContactRecord item = new ContactRecord
            {
                Name = (name ?? "").Trim(),
                IpAddress = (ip ?? "").Trim(),
                Note = (note ?? "").Trim(),
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            _items.Add(item);
            Save();
            return item;
        }

        public void Update(ContactRecord item)
        {
            if (item == null) return;
            item.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Save();
        }

        public void Delete(string id)
        {
            _items.RemoveAll(x => x.Id == id);
            Save();
        }

        public ContactRecord FindById(string id)
        {
            return _items.Find(x => x.Id == id);
        }

        private static string SerializeList(List<ContactRecord> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[\r\n");
            for (int i = 0; i < items.Count; i++)
            {
                ContactRecord c = items[i];
                sb.Append("  {");
                sb.Append("\"Id\":\"").Append(Escape(c.Id)).Append("\",");
                sb.Append("\"Name\":\"").Append(Escape(c.Name)).Append("\",");
                sb.Append("\"IpAddress\":\"").Append(Escape(c.IpAddress)).Append("\",");
                sb.Append("\"Note\":\"").Append(Escape(c.Note)).Append("\",");
                sb.Append("\"UpdatedAt\":\"").Append(Escape(c.UpdatedAt)).Append("\"");
                sb.Append("}");
                if (i < items.Count - 1) sb.Append(",");
                sb.Append("\r\n");
            }
            sb.Append("]\r\n");
            return sb.ToString();
        }

        private static List<ContactRecord> DeserializeList(string json)
        {
            List<ContactRecord> list = new List<ContactRecord>();
            if (string.IsNullOrWhiteSpace(json)) return list;

            MatchCollection objects = Regex.Matches(json, "\\{[^}]+\\}");
            foreach (Match m in objects)
            {
                string obj = m.Value;
                ContactRecord c = new ContactRecord
                {
                    Id = Unescape(Extract(obj, "Id")),
                    Name = Unescape(Extract(obj, "Name")),
                    IpAddress = Unescape(Extract(obj, "IpAddress")),
                    Note = Unescape(Extract(obj, "Note")),
                    UpdatedAt = Unescape(Extract(obj, "UpdatedAt"))
                };
                if (string.IsNullOrEmpty(c.Id))
                {
                    c.Id = Guid.NewGuid().ToString("N");
                }
                list.Add(c);
            }
            return list;
        }

        private static string Extract(string obj, string key)
        {
            Match m = Regex.Match(obj, "\"" + key + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Unescape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
