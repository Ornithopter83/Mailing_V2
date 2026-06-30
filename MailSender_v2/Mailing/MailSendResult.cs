namespace MailSender_v2.Mailing
{
    internal sealed class MailSendResult
    {
        public RecipientListItem Recipient { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
