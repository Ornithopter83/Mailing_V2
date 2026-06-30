using System;

namespace MailSender_v2.Mailing
{
    internal sealed class RecipientListItem
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string AgencyName { get; set; }
        public DateTime? NoticeDate { get; set; }
    }
}
