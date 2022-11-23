using Acrelec.Library.Logger;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Acrelec.Mockingbird.Payment.Configuration
{
    public class AppConfiguration
    {
        private class ConfigurationEntry
        {
            public string Key { get; set; }

            public string Section { get; set; }

            public string Value { get; set; }
        }

        static AppConfiguration()
        {
            Instance = new AppConfiguration();
        }

        private IList<ConfigurationEntry> _entries;

        private AppConfiguration(string iniPath = null)
        {
            var executingAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var path = new FileInfo(iniPath ?? executingAssemblyName + ".ini").FullName;
            ParseFile(path);
        }

        private void ParseFile(string path)
        {
            _entries = new List<ConfigurationEntry>();

            if (!File.Exists(path))
            {
                Log.Info($"Configuration file {path} not found. Using default values.");
                return;
            }
            else
            {
                Log.Info($"Configuration file: {path} found.");
            }

            var section = string.Empty;
            foreach (var line in File.ReadAllLines(path))
            {
                var sectionMatch = Regex.Match(line, @"^\[(\w+)\]\s*$");
                if (sectionMatch.Success)
                {
                    section = sectionMatch.Groups[1].Value;
                    continue;
                }

                var entryMatch = Regex.Match(line, @"^([^;]+?)=([\s\S]+?)$");
                if (entryMatch.Success)
                {
                    _entries.Add(new ConfigurationEntry()
                    {
                        Section = section,
                        Key = entryMatch.Groups[1].Value,
                        Value = entryMatch.Groups[2].Value
                    });
                }
            }
        }

        public string OutPath
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key =="OUT_PATH")?.Value ?? @"out\";
            }
        }
        public string Port
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "PORT")?.Value ?? "8000";
            }
        }

        public string Currency
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "CURRENCY")?.Value ?? "GBP";
            }
        }

        public string Country
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "COUNTRY")?.Value ?? "GBP";
            }
        }
        public string SourceId
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "SOURCE_ID")?.Value ?? "DK01.P001";
            }
        }
        public string ConnectionString
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "CONNECTION_STRING")?.Value ?? "Data Source=DESKTOP-JGJG8M9\\SQLEXPRESS;Initial Catalog=AKD_MAB;Integrated Security=True";
            }
        }
        public string TableName
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "TABLE_NAME")?.Value ?? "DATABASETABLE";
            }
        }

        //API call settings
        public string ContentType
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "CONTENT_TYPE")?.Value ?? "text/plain";
            }
        }

        public string ApiKey1
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "API_KEY1")?.Value ?? "hdgskIZRgBmyArKCtzkjkZIvaBjMkXVbWGvbq";
            }
        }

        public string ApiKey2
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "API_KEY2")?.Value ?? "u7f2r48x6bzwyy09vwsii";
            }
        }

        public string KeyType1
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "KEY_TYPE1")?.Value ?? "X-Flyt-API-Key";
            }
        }

        public string KeyType2
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "KEY_TYPE2")?.Value ?? "X-Flypay-API-Key";
            }
        }

        public string OrderUrl
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "ORDER_URL")?.Value ?? "https://api.flypaythis.com/ordering/v3/order";
            }
        }

        public string MarkAsPaidUrl
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "MARKASPAID_URL")?.Value ?? "/mark-as-paid";
            }
        }

        public string SendToPosUrl
        {
            get
            {
                return _entries.FirstOrDefault(_ => _.Key == "SENDTOPOS_URL")?.Value ?? "https://flyt-acrelec-integration.flyt-platform.com/sendToPo";
            }
        }

        public int HeartbeatInterval
        {
            get
            {
                var entry = _entries.FirstOrDefault(_ => _.Key == "HEARTBEAT_INTERVAL")?.Value;
                return int.TryParse(entry, out var result) ? result : 300;
            }
        }

        public static AppConfiguration Instance { get; }
    }
}
