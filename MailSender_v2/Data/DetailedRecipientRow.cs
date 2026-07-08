using System;

namespace MailSender_v2.Data
{
    internal sealed class DetailedRecipientRow
    {
        public bool IsSelected { get; set; }
        public long Id { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string AgencyName { get; set; }
        public DateTime? NoticeDate { get; set; }
        public string NoticeName { get; set; }
        public string ManagerName { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
        public DateTime? LastProcessedAt { get; set; }
        public string BlockedReason { get; set; }
        public DateTime? BlockedCreatedAt { get; set; }
    }
}
