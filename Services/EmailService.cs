using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace VibrationMonitorReservation.Services
{
    //Sähköpostiviesti palvelu mm. varausten jättöä ja kuittaamista varten
    public class EmailSettings
    {
        public string MailAddress { get; set; }
        public string MailDisplayName { get; set; }
        public string Password { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
    }
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;
        public EmailService(IOptionsSnapshot<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public string SendEmail(List<string> toEmail, string subject, string body)
        {
            var fromAddress = new MailAddress(_emailSettings.MailAddress, _emailSettings.MailDisplayName);
            string fromPassword = _emailSettings.Password;
            string smtpHost = _emailSettings.SmtpHost;
            int smtpPort = _emailSettings.SmtpPort;

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                // Note: only needed if the SMTP server requires authentication
                client.Authenticate(fromAddress.Address, fromPassword);

                foreach (var Email in toEmail)
                {
                    var toAddress = new MailboxAddress(Email, Email);
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(fromAddress.DisplayName, fromAddress.Address));
                    message.To.Add(toAddress);
                    message.Subject = subject;

                    message.Body = new TextPart("plain")
                    {
                        Text = body
                    };

                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception ex)
                    {
                        // Here you could log the error, or re-throw it to be handled elsewhere
                        Console.WriteLine($"Error sending email: {ex.Message}");
                        return $"Error sending email: {ex.Message}";
                    }
                }

                client.Disconnect(true);
            }

            return "Email process completed.";
        }



    }

    public class GenerateEmailPassword
    {

        private static readonly char[] LowercaseLetters = Enumerable.Range(97, 26).Select(i => (char)i).ToArray();
        private static readonly char[] UppercaseLetters = Enumerable.Range(65, 26).Select(i => (char)i).ToArray();
        private static readonly char[] Numbers = Enumerable.Range(48, 10).Select(i => (char)i).ToArray();
        private static readonly char[] SpecialCharacters = new char[] { '!', '_', '-', '#'};

        public string GenerateNewEmailPassword(int length = 8)
        {
            if (length < 8) throw new ArgumentException("Length must be at least 8.");

            var rng = new RNGCryptoServiceProvider();

            var password = new StringBuilder(length);

            password.Append(GetRandomCharacter(UppercaseLetters, rng));
            password.Append(GetRandomCharacter(LowercaseLetters, rng));
            password.Append(GetRandomCharacter(Numbers, rng));
            password.Append(GetRandomCharacter(SpecialCharacters, rng));

            while (password.Length < length)
            {
                var allChars = LowercaseLetters.Concat(UppercaseLetters).Concat(Numbers).Concat(SpecialCharacters).ToArray();
                password.Append(GetRandomCharacter(allChars, rng));
            }
            return password.ToString();
        }

        private static char GetRandomCharacter(char[] characterSet, RNGCryptoServiceProvider rng)
        {
            byte[] randomByte = new byte[1];

            do
            {
                rng.GetBytes(randomByte);
            }
            while (!characterSet.Any(c => (byte)c == randomByte[0]));

            return characterSet.First(c => (byte)c == randomByte[0]);
        }
    }

}


