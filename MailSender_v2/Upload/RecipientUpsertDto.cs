using System;

namespace MailSender_v2.Upload
{
    internal sealed class RecipientUpsertDto
    {
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string NoticeNumber { get; set; }
        public string AgencyName { get; set; }
        public string DemandAgencyName { get; set; }
        public DateTime? NoticeDate { get; set; }
        public string NoticeName { get; set; }
        public string ManagerName { get; set; }
        public string Phone { get; set; }
        public decimal? BudgetAmount { get; set; }
        public DateTime? LastUploadedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
