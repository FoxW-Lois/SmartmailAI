using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartmailAI.Core.Data;

public class CreateGuid
{
	public static Guid DeterministicGuid(params string[] values)
	{
		string input = string.Join("|", values);

		byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));

		return new Guid(hash);
	}
}
