# SmartmailAI
  
## Description
  Smarmail est une application de gestion de boite mails (sécurisée...) intégrant des outils IA tel que la traduction automatique, le résumé de contenu et la génération automatique de réponses.

## Objectifs
  Concurencer les grosses sociétés (GAFAM) et proposer une solution abordable sécurisée et pérenne pour les TPE/PME.
  
## Installation utilisateur
  L'idée est de récupérer le fichier package d'installation Windows (x64) **.msix** et lancer. Pour le récupérer/en obtenir un, il est nécessaire de cloner ce repository, et suivre la documentation *Comment build un fichier d’installation Windows 10.11 (.NET 9 &+, WinUI 3...).doxc* disponible dans le dossier "/Documentation".

## Installation (dèv/lancement en débug)
  Afin de lancer le projet en débug, build un package package d'installation, ou bien encore continuer le développement, il est nécessaire de réaliser cette étape.
	Pour cela il faut un PC Windows 10/11 (11 x64 bits de préférence), installer Visual Studio 2026 Community ***https://visualstudio.microsoft.com/insiders/***. Une fois ceci, il faudra également installer la charge de travail **Développement d'applications WinUI**.

## Utilisation


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
  A VENIR : hashage + cryptage de toutes les données présentes en BDD
  
## Équipe
  - Nicolas Thomas
  - Loïs Pujol-Toureillat
  - Alexandre Ribes
  - Matis Missana
  - Tom Grout
  
