using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartmailAI.Core.Contracts.Services.LocalSecurity;

namespace SmartmailAI.Core.Data;

public class AesValueConverter(IAesService aesService) : ValueConverter<string, string>(
	v => aesService.EncryptAsync(v).GetAwaiter().GetResult(),
	v => aesService.DecryptAsync(v).GetAwaiter().GetResult())
{
}
