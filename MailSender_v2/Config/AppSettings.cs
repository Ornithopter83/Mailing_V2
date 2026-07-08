using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MailSender_v2.Config
{
    internal sealed class AppSettings
    {
        public SupabaseSettings Supabase { get; set; } = new SupabaseSettings();
        public string SmtpUser { get; set; } = "";
        public string SmtpPw { get; set; } = "";
        public string SmtpHost { get; set; } = "smtps.hiworks.com";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpEnableSsl { get; set; } = true;
        public string Subject { get; set; } = "입찰공고 안내드립니다";
        public string DefaultTo { get; set; } = "";
        public string DefaultCc { get; set; } = "";
        public List<string> Body { get; set; } = CreateDefaultBodyLines();
        public List<MailImageSetting> Images { get; set; } = new List<MailImageSetting>();
        public List<string> Download { get; set; } = new List<string>();
        public int SendInterval { get; set; } = 5000;
        public int MaxCount { get; set; } = 100;
        public DetailSearchSettings DetailSearch { get; set; } = new DetailSearchSettings();

        [JsonIgnore]
        public string BodyText => Body != null && Body.Count > 0
            ? string.Join(Environment.NewLine, Body)
            : "";

        public static AppSettings LoadOrCreate(string configPath)
        {
            if (!File.Exists(configPath))
            {
                var created = new AppSettings();
                created.Save(configPath);
                return created;
            }

            var json = File.ReadAllText(configPath);
            var loaded = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            var shouldSave = loaded.DetailSearch == null;
            loaded.Supabase = loaded.Supabase ?? new SupabaseSettings();
            loaded.SmtpUser = loaded.SmtpUser ?? "";
            loaded.SmtpPw = loaded.SmtpPw ?? "";
            loaded.SmtpHost = string.IsNullOrWhiteSpace(loaded.SmtpHost) ? "smtps.hiworks.com" : loaded.SmtpHost;
            loaded.Subject = string.IsNullOrWhiteSpace(loaded.Subject) ? "입찰공고 안내드립니다" : loaded.Subject;
            loaded.DefaultTo = loaded.DefaultTo ?? "";
            loaded.DefaultCc = loaded.DefaultCc ?? "";
            loaded.Body = loaded.Body ?? new List<string>();
            if (loaded.Body.Count == 0)
            {
                loaded.Body = CreateDefaultBodyLines();
            }

            loaded.Images = loaded.Images ?? new List<MailImageSetting>();
            loaded.Download = loaded.Download ?? new List<string>();
            loaded.DetailSearch = loaded.DetailSearch ?? new DetailSearchSettings();
            loaded.DetailSearch.ApplyDefaults();
            loaded.SendInterval = loaded.SendInterval <= 0 ? 5000 : loaded.SendInterval;
            loaded.MaxCount = loaded.MaxCount <= 0 ? 100 : loaded.MaxCount;
            if (loaded.SmtpPort <= 0)
            {
                loaded.SmtpPort = 587;
            }

            if (shouldSave)
            {
                loaded.Save(configPath);
            }

            return loaded;
        }

        public void Save(string configPath)
        {
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(configPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        private static List<string> CreateDefaultBodyLines()
        {
            return new List<string>
            {
                "안녕하세요.",
                "",
                "아래와 같이 입찰공고를 안내드립니다.",
                "감사합니다.",
            };
        }
    }

    internal sealed class DetailSearchSettings
    {
        public string NoticeDateFrom { get; set; } = "";
        public string NoticeDateTo { get; set; } = "";
        public bool IncludeUnsent { get; set; } = true;
        public bool IncludeSent { get; set; } = true;
        public bool IncludeBlocked { get; set; } = true;
        public int MaxCount { get; set; } = 1000;
        public bool ExportAllRows { get; set; }

        public void ApplyDefaults()
        {
            NoticeDateFrom = NoticeDateFrom ?? "";
            NoticeDateTo = NoticeDateTo ?? "";
            MaxCount = MaxCount <= 0 ? 1000 : MaxCount;
        }
    }

    internal sealed class MailImageSetting
    {
        [JsonProperty("ID")]
        public string Id { get; set; }
        public string FileName { get; set; }
        public string Width { get; set; }
    }

    internal sealed class SupabaseSettings
    {
        public string Url { get; set; } = "https://zogwawbwtxkogmgdvskb.supabase.co";
        public string AnonKey { get; set; } = "";

        [JsonIgnore]
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Url) &&
            !string.IsNullOrWhiteSpace(AnonKey);

        [JsonIgnore]
        public string NormalizedUrl => (Url ?? "").Trim().TrimEnd('/');
    }
}
