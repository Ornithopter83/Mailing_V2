using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailSender_v2.Config;

namespace MailSender_v2.Mailing
{
    internal sealed class MailSendService
    {
        public async Task<List<MailSendResult>> SendAsync(
            IReadOnlyList<RecipientListItem> recipients,
            MailDraft draft,
            AppSettings settings,
            Action<string> log,
            CancellationToken cancellationToken)
        {
            if (recipients == null || recipients.Count == 0)
            {
                throw new InvalidOperationException("발송 대상이 없습니다.");
            }

            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(settings.SmtpUser) ||
                string.IsNullOrWhiteSpace(settings.SmtpPw))
            {
                throw new InvalidOperationException("SMTP 설정이 비어 있습니다. config.json의 SmtpHost, SmtpUser, SmtpPw를 확인하세요.");
            }

            var results = new List<MailSendResult>();
            using (var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort))
            {
                client.EnableSsl = settings.SmtpEnableSsl;
                client.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPw);

                for (var index = 0; index < recipients.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var recipient = recipients[index];
                    try
                    {
                        using (var message = CreateMessage(recipient, draft, settings.SmtpUser))
                        {
                            log?.Invoke($"[SMTP] 발송 시작: {recipient.Email}");
                            await client.SendMailAsync(message).ConfigureAwait(false);
                        }

                        results.Add(new MailSendResult { Recipient = recipient, IsSuccess = true, Message = "Sent" });
                        log?.Invoke($"[SMTP] 발송 성공: {recipient.Email}");
                    }
                    catch (Exception ex)
                    {
                        results.Add(new MailSendResult { Recipient = recipient, IsSuccess = false, Message = ex.Message });
                        log?.Invoke($"[SMTP] 발송 실패: {recipient.Email} - {ex.Message}");
                    }

                    if (index < recipients.Count - 1 && settings.SendInterval > 0)
                    {
                        await Task.Delay(settings.SendInterval, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return results;
        }

        public static string SaveReport(IEnumerable<MailSendResult> results, string directory)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"Report_{DateTime.Now:yyMMdd-HHmm}.txt");
            var builder = new StringBuilder();
            builder.AppendLine($"발송 결과 보고서: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();

            foreach (var result in results)
            {
                builder.AppendLine($"{(result.IsSuccess ? "성공" : "실패")}\t{result.Recipient?.Email}\t{result.Message}");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
        }

        private static MailMessage CreateMessage(RecipientListItem recipient, MailDraft draft, string sender)
        {
            var message = new MailMessage();
            message.From = new MailAddress(sender);
            message.To.Add(recipient.Email);
            message.Subject = draft.Subject;
            message.Body = ConvertBodyToHtml(draft.GetBodyText(), draft.Images);
            message.IsBodyHtml = true;

            foreach (var cc in SplitAddresses(draft.DefaultCc))
            {
                message.CC.Add(cc);
            }

            foreach (var attachmentPath in draft.AttachmentPaths.Where(File.Exists))
            {
                message.Attachments.Add(new Attachment(attachmentPath));
            }

            AddInlineImages(message, draft.Images);

            return message;
        }

        private static IEnumerable<string> SplitAddresses(string value)
        {
            return (value ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0);
        }

        private static string ConvertBodyToHtml(string text, IEnumerable<MailImageSetting> images)
        {
            var encoded = WebUtility.HtmlEncode(text ?? "");
            var html = encoded.Replace("\r\n", "\n").Replace("\n", "<br>");

            foreach (var image in images ?? Enumerable.Empty<MailImageSetting>())
            {
                if (string.IsNullOrWhiteSpace(image.Id))
                {
                    continue;
                }

                var width = "";
                if (int.TryParse(image.Width, out var parsedWidth) && parsedWidth > 0)
                {
                    width = $" width=\"{parsedWidth}\"";
                }

                html = html.Replace(
                    "{" + WebUtility.HtmlEncode(image.Id) + "}",
                    $"<img src=\"cid:{WebUtility.HtmlEncode(image.Id)}\"{width}>");
            }

            return html;
        }

        private static void AddInlineImages(MailMessage message, IEnumerable<MailImageSetting> images)
        {
            var imageList = (images ?? Enumerable.Empty<MailImageSetting>())
                .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.FileName))
                .ToList();
            if (imageList.Count == 0)
            {
                return;
            }

            var view = AlternateView.CreateAlternateViewFromString(message.Body, Encoding.UTF8, MediaTypeNames.Text.Html);
            var hasResource = false;
            foreach (var image in imageList)
            {
                var path = ResolveRuntimePath(image.FileName);
                if (!File.Exists(path))
                {
                    continue;
                }

                var resource = new LinkedResource(path)
                {
                    ContentId = image.Id,
                    TransferEncoding = TransferEncoding.Base64,
                };
                resource.ContentType.MediaType = GetImageMediaType(path);
                view.LinkedResources.Add(resource);
                hasResource = true;
            }

            if (hasResource)
            {
                message.AlternateViews.Add(view);
            }
            else
            {
                view.Dispose();
            }
        }

        private static string ResolveRuntimePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        private static string GetImageMediaType(string path)
        {
            switch (Path.GetExtension(path)?.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return MediaTypeNames.Image.Jpeg;
                case ".gif":
                    return MediaTypeNames.Image.Gif;
                default:
                    return "image/png";
            }
        }
    }
}
