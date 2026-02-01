using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

class Program
{
    static async Task Main()
    {
        // =================== Connexion OAuth2 ===================
        var clientSecrets = new ClientSecrets
        {
            ClientId = "687689133134-p1h6di4c2chv5dne4rfi3cfljp0ln9n8.apps.googleusercontent.com",
            ClientSecret = "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"
        };

        var scopes = new[] { GmailService.Scope.GmailReadonly };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets,
            scopes,
            "user",
            CancellationToken.None
        );

        Console.WriteLine("Connexion réussie !");
        Console.WriteLine($"Access Token: {credential.Token.AccessToken}\n");

        // =================== Liste des 50 derniers mails ===================
        await ListLast50GmailEmailsAsync(credential);

        // =================== Demande de déconnexion ===================
        Console.WriteLine("\nVoulez-vous vous déconnecter complètement ? (o/n)");
        var answer = Console.ReadLine();

        if (answer?.ToLower() == "o")
        {
            await LogoutAsync(credential);
        }
    }

    static async Task ListLast50GmailEmailsAsync(UserCredential credential)
    {
        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "MailOAuthTester"
        });

        var request = service.Users.Messages.List("me");
        request.MaxResults = 50;
        request.LabelIds = "INBOX";
        request.IncludeSpamTrash = false;

        ListMessagesResponse response = await request.ExecuteAsync();

        if (response.Messages == null || response.Messages.Count == 0)
        {
            Console.WriteLine("Aucun message trouvé.");
            return;
        }

        Console.WriteLine("=== 50 derniers emails ===\n");

        foreach (var msg in response.Messages)
        {
            var fullMessage = await service.Users.Messages.Get("me", msg.Id).ExecuteAsync();

            string subject = fullMessage.Payload.Headers
                .FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(Sans objet)";
            string from = fullMessage.Payload.Headers
                .FirstOrDefault(h => h.Name == "From")?.Value ?? "(Inconnu)";

            Console.WriteLine($"{from} | {subject}");

            string body = GetMessageBody(fullMessage);
            Console.WriteLine("---- CONTENU ----");
            Console.WriteLine(body);
            Console.WriteLine("-----------------\n");
        }
    }

    // =================== Déconnexion complète ===================
    static async Task LogoutAsync(UserCredential credential)
    {
        Console.WriteLine("\nDéconnexion en cours...");

        //Révocation du token côté Google
        if (!string.IsNullOrEmpty(credential.Token.RefreshToken))
        {
            using var http = new HttpClient();
            var response = await http.PostAsync(
                "https://oauth2.googleapis.com/revoke",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(
                        "token",
                        credential.Token.RefreshToken
                    )
                })
            );

            Console.WriteLine(response.IsSuccessStatusCode
                ? "Token révoqué côté Google."
                : "Échec de la révocation du token.");
        }

        //Suppression des tokens locaux
        var tokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Google.Apis.Auth"
        );

        if (Directory.Exists(tokenPath))
        {
            Directory.Delete(tokenPath, true);
            Console.WriteLine("Tokens locaux supprimés.");
        }

        Console.WriteLine("Déconnexion complète effectuée.");
    }

    static string GetMessageBody(Message message)
    {
        // pas de multipart
        if (message.Payload.Body != null && !string.IsNullOrEmpty(message.Payload.Body.Data))
        {
            return DecodeBase64(message.Payload.Body.Data);
        }

        // Cas multipart
        if (message.Payload.Parts != null)
        {
            foreach (var part in message.Payload.Parts)
            {
                // Texte brut
                if (part.MimeType == "text/plain" && part.Body?.Data != null)
                {
                    return DecodeBase64(part.Body.Data);
                }

                // Sinon HTML
                if (part.MimeType == "text/html" && part.Body?.Data != null)
                {
                    return DecodeBase64(part.Body.Data);
                }
            }
        }

        return "(Contenu du message non trouvé)";
    }

    static string DecodeBase64(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Gmail utilise Base64 URL-safe
        input = input.Replace('-', '+').Replace('_', '/');
        var bytes = Convert.FromBase64String(input);
        return Encoding.UTF8.GetString(bytes);
    }
}
