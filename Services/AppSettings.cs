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
	private static readonly string DefaultFilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"jdownloader Stats",
		"settings.json");

	public static AppSettings Load() => Load(DefaultFilePath);

	public static void Save(AppSettings settings) => Save(settings, DefaultFilePath);

	public static AppSettings Load(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new AppSettings();
		}

		try
		{
			return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath)) ?? new AppSettings();
		}
		catch (JsonException)
		{
			return new AppSettings();
		}
	}

	public static void Save(AppSettings settings, string filePath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
		File.WriteAllText(filePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
	}
}
