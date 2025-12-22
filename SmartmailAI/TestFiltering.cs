using System;
using System.Collections.Generic;
using System.Linq;
using MailFiltering;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== MAIL FILTERING TESTS ===\n");

        var mails = SeedMails();

        Test(
            name: "Filtre simple sur expéditeur",
            expectedCount: 1,
            mails,
            new TextContainsFilter(m => m.From, "paul")
        );

        Test(
            name: "Filtre AND (from + pièce jointe)",
            expectedCount: 1,
            mails,
            new FilterGroup(
                LogicalOperator.And,
                new TextContainsFilter(m => m.From, "paul"),
                new BooleanFilter(m => m.HasAttachments, true)
            )
        );

        Test(
            name: "Filtre OR (paul OU spam)",
            expectedCount: 2,
            mails,
            new FilterGroup(
                LogicalOperator.Or,
                new TextContainsFilter(m => m.From, "paul"),
                new TextContainsFilter(m => m.Subject, "million")
            )
        );

        Test(
            name: "Filtre NOT (exclure spam)",
            expectedCount: 1,
            mails,
            new NotFilter(
                new TextContainsFilter(m => m.Subject, "million")
            )
        );

        Test(
            name: "Filtre date (7 derniers jours)",
            expectedCount: 1,
            mails,
            new DateAfterFilter(DateTime.Now.AddDays(-7))
        );

        Console.WriteLine("\n✅ Tous les tests sont passés avec succès.");
    }

    // =========================
    // TEST
    // =========================
    static void Test(
        string name,
        int expectedCount,
        List<MailMessage> mails,
        IMailFilter filter)
    {
        var result = MailSearch.Apply(mails, filter).ToList();

        if (result.Count != expectedCount)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ECHEC : {name}");
            Console.ResetColor();

            Console.WriteLine($"Attendu : {expectedCount}");
            Console.WriteLine($"Obtenu : {result.Count}");
            Console.WriteLine("Résultats obtenus :");

            foreach (var mail in result)
                Console.WriteLine($"- {mail.From} | {mail.Subject}");

            Environment.Exit(1);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✔ {name}");
        Console.ResetColor();
    }

    // =========================
    // DONNEES DE TEST
    // =========================
    static List<MailMessage> SeedMails() => new()
    {
        new MailMessage
        {
            From = "paul@example.com",
            Subject = "Facture EDF",
            Date = DateTime.Now.AddDays(-2),
            HasAttachments = true,
            IsRead = false,
            Size = 300_000
        },
        new MailMessage
        {
            From = "spam@pub.com",
            Subject = "Gagne 1 million",
            Date = DateTime.Now.AddDays(-1),
            HasAttachments = false,
            IsRead = false,
            Size = 50_000
        },
        new MailMessage
        {
            From = "marie@example.com",
            Subject = "Réunion",
            Date = DateTime.Now.AddDays(-30),
            HasAttachments = false,
            IsRead = true,
            Size = 80_000
        }
    };
}
