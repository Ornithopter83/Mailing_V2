using System.Collections.Generic;

namespace MailSender_v2.Mailing
{
    internal sealed class MailDraft
    {
        public string DefaultTo { get; set; }
        public string DefaultCc { get; set; }
        public string Subject { get; set; }
        public string DefaultBodyText { get; set; }
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }
}
