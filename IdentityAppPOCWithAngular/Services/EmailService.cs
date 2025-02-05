using IdentityAppPOCWithAngular.DTOs.Account;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using System.Net;
using System.Net.Mail;

namespace IdentityAppPOCWithAngular.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(EmailSendDto emailSendDto)
        {
            MailjetClient client = new MailjetClient(_configuration["MailJet:ApiKey"], _configuration["MailJet:SecretKey"]);

            var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(_configuration["Email:From"], _configuration["Email:ApplicationName"]))
                .WithSubject(emailSendDto.Subject)
                .WithHtmlPart(emailSendDto.Body)
                .WithTo(new SendContact(emailSendDto.To))
                .Build();

            var response = await client.SendTransactionalEmailAsync(email);
            if (response.Messages != null)
            {
                if (response.Messages[0].Status == "success")
                {
                    return true;
                }
            }
            return false;
        }

        //public async Task<bool> SendEmailAsync(EmailSendDto emailSendDto)
        //{
        //    try
        //    {
        //        var username = _configuration["SMTP:username"];
        //        var password = _configuration["SMTP:password"];

        //        var smtpClient = new SmtpClient("smtp-mail.outlook.com", 587)
        //        {
        //            EnableSsl = true,
        //            Credentials = new NetworkCredential(username, password)
        //        };

        //        var message = new MailMessage(from: username, to: emailSendDto.To, subject: emailSendDto.Subject, body: emailSendDto.Body);

        //        message.IsBodyHtml = true;  
        //        await smtpClient.SendMailAsync(message);
        //        return true;
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        return false;
        //    }
        //}
    }
}
