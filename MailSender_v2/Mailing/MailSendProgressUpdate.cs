namespace MailSender_v2.Mailing
{
    internal enum MailSendProgressState
    {
        Sending,
        Success,
        Failure,
    }

    internal sealed class MailSendProgressUpdate
    {
        public RecipientListItem Recipient { get; set; }
        public MailSendProgressState State { get; set; }
        public string Message { get; set; }
    }
}
