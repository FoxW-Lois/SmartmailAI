# SmartmailAI
  
## Description
  Smarmail est une application de gestion de boite mails sécurisée intégrant des outils IA tel que la traduction automatique, le résumé de contenu et la génération automatique de réponses.

## Objectifs
  Concurencer les grosses sociétés (GAFAM) et proposer une solution abordable sécurisée et pérenne pour les TPE/PME.
  
## Installation utilisateur
  L'idée est de récupérer le fichier package d'installation Windows (x64) **.msix** et lancer. Pour le récupérer/en obtenir un, il est nécessaire de cloner ce repository, et suivre la documentation *Comment build un fichier d’installation Windows 10.11 (.NET 9 &+, WinUI 3...).doxc* disponible dans le dossier "/Documentation".

## Installation (dèv/lancement en débug)
  Afin de lancer le projet en débug, build un package package d'installation, ou bien encore continuer le développement, il est nécessaire de réaliser cette étape.
	Pour cela il faut un PC Windows 10/11 (11 x64 bits de préférence), installer Visual Studio 2026 Community ***https://visualstudio.microsoft.com/insiders/***. Une fois ceci, il faudra également installer la charge de travail **Développement d'applications WinUI**.

## Utilisation
  - Une fois l'application lancée, il est nécessaire de s'authentifier afin d'accéder aux diverses fonctionnalités du projet. Soit on choisit de s'inscrire (création d'un compte qui à l'avenir sera par défaut désactivé, en attendant qu'il soit validé par un administrateur, lui-même ayant vérifié la licence du dit utilisateur), soit on choisit de se connecter. Actuellement le compte de test est **Bob** et a pour mot de passe **123**.  
  - Il est possible de changer de langue, de theme, de colorscheme ou encore d'activer la double authentification avec Google Authenticator en passant par la page des **paramètres**.  
  - La page **Liste de détails** est une page utile au développement et gère l'affichage, l'envoi, la modification d'état, le filtrage et le rangement des emails. Dans cette version de développement, les emails sont fictifs et les (vrais) emails récupérés par la connexion d'une adresse email au projet a été désactivée (lignes commentées dans le fichier *EmailsService.cs*).  
  - La page **Ajouter une adresse** permet de connecter plusieurs adresses email des utilisateurs au projet. *Actuellement* il est possible de connecter tout type d'adresses *mais uniquement* en passant par la méthode de connexion de Google. La connexion par les services de Microsoft et par les services SMTP/IMAP/POP3 sera mise en place prochainement.  
  - La page **Gérer les adresses** donne la possibilité de supprimer les adresses emails (et leurs credendials) connectées au projet ainsi que tous les emails récupérés, liés à celles-ci.

## Architecture
Le projet s'organise autour de l'architecture/méthode de conception MVVM (Model–view–viewmodel). La solution SmartmailAI.sln comporte 3 sous-projets afin de séparer les responsabilités et de regrouper le code par types d'opérations :  
  - SmartmailAI : Organise l'interface et l'expérience utilisateur (navigation, themes, langues, paramètres utilisateur...)  
  - SmartmailAI.Core : Organise et regroupe toutes les opérations relatives à la gestion des données (credentials, base de données, état des emails...)  
  - SmartmailAI.Infrastructure : Gère tout ce qui est relatif à l'écosystème WinUI3

<img width="645" height="512" alt="Schema_darchitecture_technique drawio" src="https://github.com/user-attachments/assets/443fd0b7-d741-4729-a01e-62ccc7d83ac0" />

L'utilisateur de l'application va se connecter avec un compte et utiliser Google Authenticator pour la double authentification. Ensuite quand il va ajouter un mail, l'utilisateur utilisera un serveur SMTP ou l'API Google pour intégrer sa boite mail et ses mails correspondant pour les intégrer dans l'application. Les mails de l'utilisateur seront ensuite enregistrés dans la base de données SQLite. 
Pour qu'un utilisateur puisse se connecter, il faut qu'une licence soit disponible, et ces informations concernant la licence seront enregistrées dans une base externe MariaDB. Celle-ci peut permettre de bloquer à distance l'utilisation de l'application si par exemple un client ne renouvelle pas sa licence, ou bien récupérer le package d'installation sans en avoir payé une.
  
## Sécurité et RGPD
  - Double authentification  
  - Filtrage et détection de phishings
  - Il est possible de déplacer/sortir manuellement par clic droit un email vers/de 'PhishingSpam'. Lorsque ces actions sont effectués, l'adresse email du message de l'envoyeur est ainsi notée en base de données comme appartenant à une whitelist ou blacklist. L'appartenance à la whitelist permet d'ignorer l'étape de check du spoofing du nom affiché, et la blacklist permet de directement passer ce check avec la certitude qu'il y ait spoofing.
  - Hashage + salage du mot de passe des utilisateurs  
  - Chiffrement de toutes les données sensibles/confidentielles dans la BDD locale SQLite :
    - Numéros de téléphone associés aux comptes Smartmail
    - Adresses emails connectées à l'application : l'Adresse Email, le TokenStorageKey, le GoogleUserId (dans le cas d'une connexion par les services de Google), ainsi que le UserName, ImapHost et SmtpHost (dans le cas d'une connexion via méthode IMAP/SMTP)
    - Emails (messages) : l'Adresse Email et le Nom de l'envoyeur et du réceptionnaire, les Cc et Cci, l'Objet, le Contenu et si il y en a les Pièces Jointes associées
  - Chiffrement SSL/TLS (même chose, juste 2 appellations) lors de l'envoi et de la réception d'Emails
  - La clé de chiffrement et la session (permettant de rester connecté un certain temps à l'application après fermeture) sont toutes deux chiffrées et stockées de manière sécurisé via le Windows DPAPI (impossible de déchiffrer si l'utilisateur ayant émis la clé et la session n'a pas déverouillé sa machine avec la bonne session utilisateur)
  
## Équipe
  - Nicolas Thomas
  - Loïs Pujol-Toureillat
  - Alexandre Ribes
  - Matis Missana
  - Tom Grout
