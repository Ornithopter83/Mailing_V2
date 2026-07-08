using System;

namespace MailSender_v2.Data
{
    internal sealed class DetailedRecipientSearchOptions
    {
        public DateTime? NoticeDateFrom { get; set; }
        public DateTime? NoticeDateTo { get; set; }
        public bool IncludeUnsent { get; set; }
        public bool IncludeSent { get; set; }
        public bool IncludeBlocked { get; set; }
        public int MaxCount { get; set; }
        public string NoticeDateFromText { get; set; }
        public string NoticeDateToText { get; set; }

        public bool IncludesAllStatuses => !IncludeUnsent && !IncludeSent && !IncludeBlocked;
    }
}
