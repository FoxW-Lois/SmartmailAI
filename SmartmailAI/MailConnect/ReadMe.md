# SmartMailConnect

Application C# (.NET) pour se connecter à une boîte mail Hotmail/Outlook via IMAP et SMTP, en utilisant la librairie MailKit.

---

## Structure du projet

| Fichier                | Rôle                                                          |
| ---------------------- | ------------------------------------------------------------- |
| `Program.cs`           | Point d'entrée — orchestre les connexions IMAP et SMTP        |
| `EmailClient.cs`       | Classe principale — contient toutes les méthodes IMAP et SMTP |
| `MailDockerApp.csproj` | Fichier de projet .NET avec les dépendances NuGet             |
| `Dockerfile`           | Configuration pour l'exécution dans un conteneur Docker       |

---

## Prérequis

**Dépendances NuGet**

- `MailKit` — client IMAP/SMTP
- `MimeKit` — construction des messages email
- `Microsoft.Identity.Client` — authentification OAuth2 Azure (usage futur)

**Activation de l'accès IMAP sur Hotmail**

Avant de lancer l'application, activer l'accès IMAP dans les paramètres du compte :

1. Se connecter sur [outlook.com](https://outlook.com)
2. Paramètres → Courrier → Synchronisation du courrier
3. Activer l'option **Accès IMAP**

> ⚠️ Si la vérification en deux étapes (2FA) est activée, il faut utiliser un **mot de passe d'application** et non le mot de passe du compte principal. À créer sur [account.microsoft.com](https://account.microsoft.com) → Sécurité.

---

## Comprendre les ports email

Les emails utilisent deux protocoles distincts : **IMAP** pour lire les mails, **SMTP** pour en envoyer. Chaque protocole dispose de plusieurs ports selon le niveau de sécurité.

### IMAP — Lecture des emails

IMAP (Internet Message Access Protocol) permet d'accéder aux emails stockés sur le serveur sans les télécharger localement.

| Port    | Sécurité           | Méthode MailKit   | Recommandation |
| ------- | ------------------ | ----------------- | -------------- |
| **993** | SSL/TLS direct     | `SslOnConnect`    | ✅ Recommandé  |
| 143     | Aucune ou STARTTLS | `StartTls / None` | ❌ Déconseillé |

Ce projet utilise : `imap-mail.outlook.com:993` avec SSL direct.

> 📌 Source Microsoft : [Paramètres IMAP Exchange Online](https://learn.microsoft.com/exchange/clients-and-mobile-in-exchange-online/pop3-and-imap4-settings-for-exchange-online)

### SMTP — Envoi des emails

SMTP (Simple Mail Transfer Protocol) est utilisé exclusivement pour envoyer des emails.

| Port    | Sécurité       | Méthode MailKit | Recommandation                              |
| ------- | -------------- | --------------- | ------------------------------------------- |
| **587** | STARTTLS       | `StartTls`      | ✅ Recommandé — standard moderne            |
| 465     | SSL/TLS direct | `SslOnConnect`  | ⚠️ Supporté — ancienne norme, encore valide |
| 25      | Aucune         | `None`          | ❌ À éviter — bloqué par la plupart des FAI |

Ce projet utilise : `smtp-mail.outlook.com:587` avec STARTTLS.

> 📌 Source Microsoft : [Paramètres SMTP Exchange Online](https://learn.microsoft.com/exchange/clients-and-mobile-in-exchange-online/pop3-and-imap4-settings-for-exchange-online)

### SSL/TLS direct vs STARTTLS — quelle différence ?

Les deux méthodes chiffrent la communication, mais à des moments différents :

| Aspect        | SSL/TLS direct (993 / 465) | STARTTLS (143 / 587)                              |
| ------------- | -------------------------- | ------------------------------------------------- |
| Chiffrement   | Immédiat dès la connexion  | Négociation après une connexion initiale en clair |
| Sécurité      | Légèrement plus simple     | Équivalent si bien configuré                      |
| Usage typique | IMAP (993)                 | SMTP (587)                                        |

En pratique, les deux sont considérés comme sécurisés. L'essentiel est de ne jamais utiliser les ports sans chiffrement (143 sans STARTTLS, 25).

---

## Utilisation

### Variables d'environnement

Les identifiants ne doivent **jamais** être écrits en dur dans le code. Ils sont passés via des variables d'environnement :

| Variable        | Description                                | Exemple               |
| --------------- | ------------------------------------------ | --------------------- |
| `MAIL_USER`     | Adresse email complète                     | `exemple@hotmail.com` |
| `MAIL_PASSWORD` | Mot de passe ou mot de passe d'application | `VotreMotDePasse`     |

### Lancement en local

```bash
MAIL_USER=votre@hotmail.com MAIL_PASSWORD=votremdp dotnet run
```

### Lancement avec Docker

```bash
docker build -t smartmailconnect .
docker run -e MAIL_USER=votre@hotmail.com -e MAIL_PASSWORD=votremdp smartmailconnect
```

---

## Méthodes disponibles (EmailClient)

| Méthode                             | Protocole | Description                                           |
| ----------------------------------- | --------- | ----------------------------------------------------- |
| `ConnectImapAsync()`                | IMAP      | Teste la connexion et l'authentification IMAP         |
| `ListEmailsAsync(n)`                | IMAP      | Liste les n derniers emails (sujet, expéditeur, date) |
| `ConnectSmtpAsync()`                | SMTP      | Teste la connexion et l'authentification SMTP         |
| `SendEmailAsync(to, subject, body)` | SMTP      | Envoie un email                                       |

---

## Résolution des erreurs courantes

**AuthenticationException — Authentification refusée**

- Vérifier que l'accès IMAP est activé dans les paramètres Outlook
- Si 2FA activé : utiliser un mot de passe d'application
- Vérifier que `MAIL_USER` et `MAIL_PASSWORD` sont bien définis

**Connexion impossible / timeout**

- Vérifier la connexion réseau
- Vérifier que les ports 993 (IMAP) et 587 (SMTP) ne sont pas bloqués par un pare-feu

---

## Sécurité

- Ne jamais écrire les identifiants en dur dans le code source
- Utiliser des variables d'environnement ou un gestionnaire de secrets (Azure Key Vault, Docker Secrets)
- Ne jamais versionner un fichier `.env` contenant des mots de passe
- Ajouter `.env` dans le `.gitignore`
- Préférer un mot de passe d'application dédié plutôt que le mot de passe principal du compte
