using System;
using System.Threading.Tasks;

namespace SmartMailConnect
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Récupération depuis les variables d'environnement (recommandé)
            // ou fallback sur les args si fournis en ligne de commande
            string email    = Environment.GetEnvironmentVariable("MAIL_USER")
                           ?? (args.Length > 0 ? args[0] : null);
            string password = Environment.GetEnvironmentVariable("MAIL_PASSWORD")
                           ?? (args.Length > 1 ? args[1] : null);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Erreur : identifiants manquants.");
                Console.WriteLine("Définissez MAIL_USER et MAIL_PASSWORD en variables d'environnement.");
                Console.WriteLine("  Exemple : MAIL_USER=xxx@hotmail.com MAIL_PASSWORD=votre_mdp dotnet run");
                return;
            }

            var client = new EmailClient(email, password);

            Console.WriteLine("=== TEST CONNEXION IMAP ===");
            await client.ConnectImapAsync();

            Console.WriteLine("\n=== LISTING DES EMAILS ===");
            await client.ListEmailsAsync(maxEmails: 10);

            Console.WriteLine("\n=== TEST CONNEXION SMTP ===");
            await client.ConnectSmtpAsync();

            //envoyer un email de test :
            Console.WriteLine("\n=== ENVOI EMAIL TEST ===");
            await client.SendEmailAsync(email, "Test SmartMailConnect", "Ça fonctionne !");
        }
    }
}