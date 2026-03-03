using System;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace SmartMailConnect
{
    public class EmailClient
    {
        // Serveurs corrects pour Hotmail/Outlook personnel
        private const string ImapHost = "imap-mail.outlook.com";
        private const int    ImapPort = 993;

        private const string SmtpHost = "smtp-mail.outlook.com";
        private const int    SmtpPort = 587;

        private readonly string _email;
        private readonly string _password;

        public EmailClient(string email, string password)
        {
            _email    = email;
            _password = password;
        }

        // ============================
        // TEST CONNEXION IMAP
        // ============================
        public async Task ConnectImapAsync()
        {
            try
            {
                using var client = new ImapClient();

                // Désactiver la vérification SSL stricte si nécessaire (test only)
                // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
                Console.WriteLine($"[IMAP] Connecté à {ImapHost}:{ImapPort}");

                await client.AuthenticateAsync(_email, _password);
                Console.WriteLine("[IMAP] Authentification réussie ✓");

                await client.DisconnectAsync(true);
                Console.WriteLine("[IMAP] Déconnecté proprement.");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                Console.WriteLine($"[IMAP] Échec d'authentification : {ex.Message}");
                Console.WriteLine("→ Vérifiez que l'accès IMAP est activé dans les paramètres Outlook.");
                Console.WriteLine("→ Utilisez un mot de passe d'application si la vérification en deux étapes est activée.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAP] Erreur : {ex.Message}");
            }
        }

        // ============================
        // LISTER LES EMAILS (IMAP)
        // ============================
        public async Task ListEmailsAsync(int maxEmails = 10)
        {
            try
            {
                using var client = new ImapClient();
                await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(_email, _password);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                Console.WriteLine($"\n[INBOX] {inbox.Count} message(s) au total.");
                Console.WriteLine($"[INBOX] Affichage des {Math.Min(inbox.Count, maxEmails)} derniers :\n");

                // On lit les plus récents en dernier
                int start = Math.Max(0, inbox.Count - maxEmails);
                for (int i = inbox.Count - 1; i >= start; i--)
                {
                    var message = await inbox.GetMessageAsync(i);
                    Console.WriteLine($"  [{i + 1}] Sujet : {message.Subject}");
                    Console.WriteLine($"       De     : {message.From}");
                    Console.WriteLine($"       Date   : {message.Date:dd/MM/yyyy HH:mm}");
                    Console.WriteLine("       ──────────────────────────────");
                }

                await client.DisconnectAsync(true);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                Console.WriteLine($"[IMAP] Échec d'authentification : {ex.Message}");
                Console.WriteLine("→ Vérifiez que l'accès IMAP est activé dans les paramètres Outlook.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAP] Erreur : {ex.Message}");
            }
        }

        // ============================
        // TEST CONNEXION SMTP
        // ============================
        public async Task ConnectSmtpAsync()
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                Console.WriteLine($"[SMTP] Connecté à {SmtpHost}:{SmtpPort}");

                await client.AuthenticateAsync(_email, _password);
                Console.WriteLine("[SMTP] Authentification réussie ✓");

                await client.DisconnectAsync(true);
                Console.WriteLine("[SMTP] Déconnecté proprement.");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                Console.WriteLine($"[SMTP] Échec d'authentification : {ex.Message}");
                Console.WriteLine("→ Vérifiez le mot de passe ou utilisez un mot de passe d'application.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP] Erreur : {ex.Message}");
            }
        }

        // ============================
        // ENVOYER UN EMAIL (SMTP)
        // ============================
        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_email, _password);

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_email));
                message.To.Add(MailboxAddress.Parse(recipient));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"[SMTP] Email envoyé à {recipient} ✓");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                Console.WriteLine($"[SMTP] Échec d'authentification : {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP] Erreur : {ex.Message}");
            }
        }
    }
}