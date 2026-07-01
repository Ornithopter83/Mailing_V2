using System;
using System.IO;
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
        public string DefaultCc { get; set; } = "";
        public string DefaultBodyText { get; set; } = "안녕하세요.\r\n\r\n아래와 같이 입찰공고를 안내드립니다.\r\n감사합니다.";
        public int SendInterval { get; set; } = 2000;
        public int MaxCount { get; set; } = 100;

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
            loaded.Supabase = loaded.Supabase ?? new SupabaseSettings();
            loaded.SmtpUser = loaded.SmtpUser ?? "";
            loaded.SmtpPw = loaded.SmtpPw ?? "";
            loaded.SmtpHost = string.IsNullOrWhiteSpace(loaded.SmtpHost) ? "smtps.hiworks.com" : loaded.SmtpHost;
            loaded.Subject = string.IsNullOrWhiteSpace(loaded.Subject) ? "입찰공고 안내드립니다" : loaded.Subject;
            loaded.DefaultCc = loaded.DefaultCc ?? "";
            loaded.DefaultBodyText = string.IsNullOrWhiteSpace(loaded.DefaultBodyText)
                ? "안녕하세요.\r\n\r\n아래와 같이 입찰공고를 안내드립니다.\r\n감사합니다."
                : loaded.DefaultBodyText;
            loaded.SendInterval = loaded.SendInterval <= 0 ? 2000 : loaded.SendInterval;
            loaded.MaxCount = loaded.MaxCount <= 0 ? 100 : loaded.MaxCount;
            if (loaded.SmtpPort <= 0)
            {
                loaded.SmtpPort = 587;
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
