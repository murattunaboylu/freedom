using System.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Freedom.Broker
{
    class ReportManager
    {
        public void SendEmail(string title, string message)
        {
            if (bool.Parse(ConfigurationManager.AppSettings["EnableEmail"]))
            {
                var client = new SendGridClient(ConfigurationManager.AppSettings["SendGridApiKey"]);
                var from = new EmailAddress("murat.tunaboylu@svarlight.com", "Freedom");
                var to = new EmailAddress("murattunaboylu@gmail.com", "Murat Tunaboylu");
                var body = message;
                var msg = MailHelper.CreateSingleEmail(from, to, title, body, "");

                var x = client.SendEmailAsync(msg);
                var r = x.Result;
            }
           
        }
    }
}
