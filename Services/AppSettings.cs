using System.IO;
using System.Text.Json;

namespace jdownloader_Stats.Services;

public sealed record AppSettings
{
	public string CloudEmail { get; init; } = "";
	public string ProtectedCloudPassword { get; init; } = "";
}

public static class SettingsStore
{
	private static readonly string FilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"jdownloader Stats",
		"settings.json");

	public static AppSettings Load()
	{
		if (!File.Exists(FilePath))
		{
			return new AppSettings();
		}

		try
		{
			return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
		}
		catch (JsonException)
		{
			return new AppSettings();
		}
	}

	public static void Save(AppSettings settings)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
		File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
	}
}
