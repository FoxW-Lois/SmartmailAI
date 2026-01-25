using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace SmartmailAI.Core.Data;

public class CheckDatabase
{
	public static async Task EnsureDatabaseAsync()
	{
		const string _dbName = "SmartmailServerDB.db";
		var localFolder = ApplicationData.Current.LocalFolder;

		var sourceFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{_dbName}"));
		await sourceFile.CopyAsync(localFolder, _dbName, NameCollisionOption.ReplaceExisting);
	}
}
