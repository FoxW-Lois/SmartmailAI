using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace SmartmailAI.Core.Data;

public class Database
{
	private const string DbName = "SmartmailDB.db";

	public static async Task EnsureDatabaseAsync()
	{
		var localFolder = ApplicationData.Current.LocalFolder;
		var dbPath = Path.Combine(localFolder.Path, DbName);

		// TODO: Commenter le if () {} en dèv si besoin de recréer la base de données à chaque démarrage
		if (!File.Exists(dbPath))
		{
			var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{DbName}"));
			await sourceFile.CopyAsync(localFolder, DbName, NameCollisionOption.ReplaceExisting);
		}
	}
}
