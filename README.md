# SmartmailAI
  
## Description
  Smarmail est une application de gestion de boite mails sécurisée intégrant des outils IA tel que la traduction automatique, le résumé de contenu et la génération automatique de réponses.

## Objectifs
  Concurencer les grosses sociétés (GAFAM) et proposer une solution abordable, sécurisée et pérenne pour les TPE/PME. [Et dans le cadre de la mise en public de ce projet, proposer une solution libre d'usage et opensource.]  
	Ce projet devient donc libre d'utilisation et de modification, mais toute redistribution modifiée doit rester libre et conserver l'attribution originale. Pour assurer cela il est indispensable de respecter la licence GNU GPL v3.
  
## Installation utilisateur
  L'idée est de récupérer le fichier package d'installation Windows (x64) **.msix** et lancer. Pour le récupérer/en obtenir un, il est nécessaire de cloner ce repository, et suivre la documentation *Comment build un fichier d’installation Windows 10.11 (.NET 9 &+, WinUI 3...).doxc* disponible dans le dossier "/Documentation".  
  Pour exploiter toutes les fonctionnalités du logiciel il est indispensable de télécharger et de lancer LM Studio, télécharger le modèle ***mistralai/ministral-3-3b***, activer le ***mode développeur***, et lancer le modèle dans l'onglet "***Developper*** > ***Local Server***"

## Installation (dèv/lancement en débug)
  Afin de lancer le projet en débug, build un package package d'installation, ou bien encore continuer le développement, il est nécessaire de réaliser cette étape :  
  Pour cela il faut un PC Windows 10/11 (11 x64 bits de préférence), installer Visual Studio 2026 Community ***https://visualstudio.microsoft.com/insiders/***. Une fois ceci, il faudra également installer la charge de travail **Développement d'applications WinUI**.  
  L'utlisation de l'extension **Todo Tree** de **Visual Studio Code** est également préférable pour avoir une vue d'ensemble sur les diverses annotations laissées aux développeurs et à une éventuelle évolution du projet.  
  Il est également nécessaire de se créer une *application* sur les services API de Google et de générer une clé API à placer au sein d'un .env local afin de gérer la connexion, récupération et envoi d'emails via les services Google. Les variables d'environnement nécessaires ainsi que le placement du .env au sein de l'arborescence du projet sont indiqués via le fichier *.envexample*.

## Utilisation
  - Une fois l'application lancée, il est nécessaire de s'authentifier afin d'accéder aux diverses fonctionnalités du projet. Soit on choisit de s'inscrire (création d'un compte qui dans un contexte de déploiement avec un serveur de license, sera par défaut désactivé en attendant d'être validé par un administrateur), soit on choisit de se connecter.
  - Lors de la première connexion à un compte, il est obligatoire de remplir un formulaire de quelques questions permettant d'améliorer au maximum l'UX (expérience utilisateur) et les performances de la machine hôte. Les réponses à ces questions sont modifiables à tout moment au sein de la page des **paramètres**.  
  - Il est possible de changer la langue, le theme, le colorscheme ou encore d'activer la double authentification avec Google Authenticator en passant par la page des **paramètres**.
  - La page **Ajouter une adresse** permet de connecter plusieurs adresses email des utilisateurs au projet. *Actuellement* il est possible de connecter tout type d'adresses en passant par les systèmes/méthode de connexion de Google, et par les services SMTP/IMAP. La connexion par les services de Microsoft a été en grande partie abandonnée en raison de la quasi impossibilité de créer une *application* sur leurs services ainsi qu'une clé API.  
  - La page **Gérer les adresses** donne la possibilité de supprimer les adresses emails (et leurs credendials) connectées au projet, ainsi que tous les emails récupérés liés à celles-ci.
  - Le menu déroulant **Adresses mails** contient toutes les adresses emails connectées au compte actuellement authentifiée au logiciel. En cliquant sur le nom d'une des adresses il sera possible d'afficher les divers emails, modifier leurs d'états (favoris, non-lu etc...), effectuer des envois, filtrer les emails, et ranger/réorganiser les emails.
    
  - Note aux **développeurs** : Il est possible d'afficher une page nommée **Liste de détails** afin d'effectuer divers tests sur des emails fictifs. Pour cela il est nécessaire suivre les instructions des lignes annontées comme : *TODO: Si besoin d'utiliser des données statiques* et *TODO: Bloc à décommenter pour l'utilisation de données statiques*. Également si on souhaite empêcher le thread parallèle d'actualisation automatisée des emails de faire des appels en continue, il est recommandé de commenter de suivre cette instruction au sein la classe-service EmailsSyncService.cs : *TODO: En dèv/debug commenter tout le contenu de la méthode pour ne pas se faire harceler à chaque appel du thread*  

## Architecture
Le projet s'organise autour de l'architecture/méthode de conception MVVM (Model–view–viewmodel). La solution SmartmailAI.sln comporte 3 sous-projets afin de séparer les responsabilités et de regrouper le code par types d'opérations :  
  - SmartmailAI : Organise l'interface et l'expérience utilisateur (navigation, themes, langues, paramètres utilisateur...)  
  - SmartmailAI.Core : Organise et regroupe toutes les opérations relatives à la gestion des données (credentials, base de données, état des emails...)  
  - SmartmailAI.Infrastructure : Gère tout ce qui est relatif à l'écosystème WinUI3

<img width="645" height="512" alt="Schema_darchitecture_technique drawio" src="/Documentation/Schéma d’architecture technique.drawio.png" />

L'utilisateur de l'application va se connecter avec un compte et utiliser Google Authenticator pour la double authentification. Ensuite quand il va ajouter un mail, l'utilisateur utilisera un serveur SMTP ou l'API Google pour intégrer sa boite mail et ses mails correspondant pour les intégrer dans l'application. Les mails de l'utilisateur seront ensuite enregistrés dans la base de données SQLite.

***[Dans un contexte de déploiement avec un serveur de license]*** Pour qu'un utilisateur puisse se connecter, il faut qu'une licence soit disponible, et ces informations concernant la licence seront enregistrées dans une base de données externe MariaDB. Celle-ci peut permettre de bloquer à distance l'utilisation de l'application si par exemple un client ne renouvelle pas sa licence, ou bien récupérer le package d'installation sans en avoir payé une.
  
## Sécurité et RGPD
  - Double authentification  
  - Filtrage et détection de phishings (+ appuie possible de l'IA à la demande de l'utilisateur, afin d'avoir un avis supplémentaire sur la présence d'un phishing/spam/fraude)
  - Il est possible de déplacer/sortir manuellement par clic droit un email vers/de 'PhishingSpam'. Lorsque ces actions sont effectués, l'adresse email du message de l'envoyeur est ainsi notée en base de données comme appartenant à une whitelist ou blacklist. L'appartenance à la whitelist permet d'ignorer l'étape de check du spoofing du nom affiché, et la blacklist permet de directement passer ce check avec la certitude qu'il y ait spoofing.
  - Hashage + salage du mot de passe des utilisateurs avec l'algorithme de hashage Argon2id (algorithme mobilisant une certaine quantité de ressources CPU et RAM afin de complètement neutraliser les attaques par bruteforce)
  - Les adresses emails connectées à un compte Smartmail ne sont consultables et supprimables, elles et leur emails, uniquement par le dit compte. Toute interference extérieure ou tentaive de vol de données est pensée comme impossible
  - Chiffrement de toutes les données sensibles/confidentielles dans la BDD locale SQLite :
    - Numéros de téléphone associés aux comptes Smartmail
    - Adresses emails connectées à l'application : l'Adresse Email, le TokenStorageKey, le GoogleUserId (dans le cas d'une connexion par les services de Google), ainsi que le UserName, ImapHost et SmtpHost (dans le cas d'une connexion via méthode IMAP/SMTP)
    - Emails (messages) : l'Adresse Email et le Nom de l'envoyeur et du réceptionnaire, les Cc et Cci, l'Objet, le Contenu et si il y en a les Pièces Jointes associées
  - Chiffrement SSL/TLS (même chose, juste 2 appellations) lors de l'envoi et de la réception d'Emails
  - La clé de chiffrement et la session (permettant de rester connecté un certain temps à l'application après fermeture) sont toutes deux chiffrées et stockées de manière sécurisé via le Windows DPAPI (impossible de déchiffrer si l'utilisateur ayant émis la clé et la session n'a pas déverouillé sa machine avec la bonne session utilisateur)
  - Le modèle d'IA (LLM) Mistral tourne purement en local, ce qui signifie qu'aucune donnée n'est envoyée ou stockée sur un serveur tiers. De plus le prompt-system bride suffisament le modèle afin qu'il ne dévoile aucune donnée autre que celles fournies par l'utilisateur.
  
## Équipe
  - Loïs Pujol-Toureillat
  - Nicolas Thomas
  - Matis Missana
  - Tom Grout
  - Alexandre Ribes
