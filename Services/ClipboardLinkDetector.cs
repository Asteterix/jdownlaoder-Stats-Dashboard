namespace jdownloader_Stats.Services;

public static class ClipboardLinkDetector
{
	public static bool TryGetHttpsUrl(string? text, out string? url)
	{
		url = null;
		var candidate = text?.Trim();
		if (string.IsNullOrEmpty(candidate) || candidate.Any(char.IsWhiteSpace))
		{
			return false;
		}

		if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
			|| uri.Scheme != Uri.UriSchemeHttps
			|| string.IsNullOrEmpty(uri.Host))
		{
			return false;
		}

		url = candidate;
		return true;
	}
}
