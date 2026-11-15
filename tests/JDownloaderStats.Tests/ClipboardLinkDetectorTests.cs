using jdownloader_Stats.Services;
using Xunit;

namespace JDownloaderStats.Tests;

public class ClipboardLinkDetectorTests
{
	[Theory]
	[InlineData("https://example.com/file.zip")]
	[InlineData("https://host.example.org:8443/path?x=1")]
	public void TryGetHttpsUrl_ReturnsTrue_ForValidHttpsUrl(string text)
	{
		var result = ClipboardLinkDetector.TryGetHttpsUrl(text, out var url);

		Assert.True(result);
		Assert.Equal(text, url);
	}

	[Fact]
	public void TryGetHttpsUrl_TrimsSurroundingWhitespace()
	{
		var result = ClipboardLinkDetector.TryGetHttpsUrl("  https://example.com/a  \r\n", out var url);

		Assert.True(result);
		Assert.Equal("https://example.com/a", url);
	}

	[Theory]
	[InlineData("http://example.com/file.zip")]
	[InlineData("ftp://example.com/file.zip")]
	[InlineData("file:///C:/temp/file.zip")]
	public void TryGetHttpsUrl_ReturnsFalse_ForNonHttpsScheme(string text)
	{
		var result = ClipboardLinkDetector.TryGetHttpsUrl(text, out var url);

		Assert.False(result);
		Assert.Null(url);
	}

	[Theory]
	[InlineData("just some text")]
	[InlineData("https://example.com/a b")]
	[InlineData("https://example.com/a\nhttps://example.com/b")]
	[InlineData("https://")]
	[InlineData("example.com/file.zip")]
	public void TryGetHttpsUrl_ReturnsFalse_ForNonUrlOrMultipleTokens(string text)
	{
		var result = ClipboardLinkDetector.TryGetHttpsUrl(text, out var url);

		Assert.False(result);
		Assert.Null(url);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void TryGetHttpsUrl_ReturnsFalse_ForNullOrBlank(string? text)
	{
		var result = ClipboardLinkDetector.TryGetHttpsUrl(text, out var url);

		Assert.False(result);
		Assert.Null(url);
	}
}
