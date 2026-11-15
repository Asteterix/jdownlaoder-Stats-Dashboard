using System.Security.Cryptography;
using System.Text;

namespace jdownloader_Stats.Services;

public static class SecretProtector
{
	public static string Protect(string plainText)
	{
		var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
		return Convert.ToBase64String(bytes);
	}

	public static string Unprotect(string protectedText)
	{
		if (string.IsNullOrEmpty(protectedText))
		{
			return "";
		}

		var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedText), null, DataProtectionScope.CurrentUser);
		return Encoding.UTF8.GetString(bytes);
	}
}
