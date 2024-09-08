using System.Net.Mail;
using CourtDocumentProcessor.Interfaces;

namespace CourtDocumentProcessor
{
    public class EmailNotifier : IEmailNotifier
    {
        private readonly string _adminEmail;

        public EmailNotifier(IAppConfigManager config)
        {
            _adminEmail = config.GetSetting("AdminEmail");
        }

        public void SendNotification(string subject, string body)
        {
            using (SmtpClient smtpClient = new SmtpClient("smtp.example.com"))
            {
                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@example.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(_adminEmail);

                //smtpClient.Send(mailMessage);
            }
        }
    }
}
