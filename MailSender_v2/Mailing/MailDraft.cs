using System;
using System.Collections.Generic;
using System.Linq;
using MailSender_v2.Config;

namespace MailSender_v2.Mailing
{
    internal sealed class MailDraft
    {
        public string DefaultTo { get; set; }
        public string DefaultCc { get; set; }
        public string Subject { get; set; }
        public List<string> Body { get; set; } = new List<string>();
        public List<MailImageSetting> Images { get; set; } = new List<MailImageSetting>();
        public List<string> Download { get; set; } = new List<string>();
        public List<string> AttachmentPaths { get; set; } = new List<string>();

        public string GetBodyText()
        {
            return Body != null && Body.Count > 0
                ? string.Join(Environment.NewLine, Body)
                : "";
        }

        public void SetBodyText(string value)
        {
            Body = (value ?? "")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .ToList();
        }
    }
}
