using System.Collections.Generic;

namespace MailSender_v2.Mailing
{
    internal sealed class MailDraft
    {
        public string Subject { get; set; }
        public string Cc { get; set; }
        public string BodyText { get; set; }
        public List<string> AttachmentPaths { get; set; } = new List<string>();
    }
}
