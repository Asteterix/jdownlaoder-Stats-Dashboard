using jdownloader_Stats.Services;
using System.IO;
using Xunit;

namespace JDownloaderStats.Tests;

public class SecretProtectorTests
{
	[Fact]
	public void Unprotect_ReturnsOriginalText_AfterProtect()
	{
		const string secret = "p@ss w0rd ✓";

		var protectedText = SecretProtector.Protect(secret);

		Assert.Equal(secret, SecretProtector.Unprotect(protectedText));
	}

	[Fact]
	public void Protect_DoesNotReturnPlainText()
	{
		const string secret = "p@ssw0rd";

		var protectedText = SecretProtector.Protect(secret);

		Assert.DoesNotContain(secret, protectedText);
	}

	[Fact]
	public void Unprotect_ReturnsEmpty_WhenInputIsEmpty()
	{
		Assert.Equal(string.Empty, SecretProtector.Unprotect(string.Empty));
	}
}

public class SettingsStoreTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "jdownloader-stats-tests", Guid.NewGuid().ToString("N"));

	private string FilePath => Path.Combine(_directory, "settings.json");

	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	[Fact]
	public void Load_ReturnsDefaults_WhenFileIsMissing()
	{
		var settings = SettingsStore.Load(FilePath);

		Assert.Equal(string.Empty, settings.CloudEmail);
		Assert.Equal(string.Empty, settings.ProtectedCloudPassword);
	}

	[Fact]
	public void Save_ThenLoad_RoundTripsSettings()
	{
		var original = new AppSettings { CloudEmail = "user@example.com", ProtectedCloudPassword = "encrypted-blob" };

		SettingsStore.Save(original, FilePath);
		var loaded = SettingsStore.Load(FilePath);

		Assert.Equal(original, loaded);
	}

	[Fact]
	public void Load_ReturnsDefaults_WhenFileIsCorrupt()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(FilePath, "{ not valid json");

		var settings = SettingsStore.Load(FilePath);

		Assert.Equal(string.Empty, settings.CloudEmail);
	}

	[Fact]
	public void Load_IgnoresLegacyKeys_FromOlderVersions()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(FilePath, """{"CloudEmail":"a@b.c","TryLocalFirst":true}""");

		var settings = SettingsStore.Load(FilePath);

		Assert.Equal("a@b.c", settings.CloudEmail);
	}

	[Fact]
	public void Load_ReturnsDefaults_WhenFileIsLocked()
	{
		Directory.CreateDirectory(_directory);
		File.WriteAllText(FilePath, "{\"CloudEmail\":\"a@b.c\"}");
		using var locked = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

		var settings = SettingsStore.Load(FilePath);

		Assert.Equal(string.Empty, settings.CloudEmail);
	}
}
