using System;

namespace MailSender_v2.Mailing
{
    internal sealed class SendHistoryDto
    {
        public long? RecipientId { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Method { get; set; }
        public string Memo { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
