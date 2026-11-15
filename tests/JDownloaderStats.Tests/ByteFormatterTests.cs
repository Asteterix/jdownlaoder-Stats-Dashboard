using System.Globalization;
using jdownloader_Stats.Services;
using Xunit;

namespace JDownloaderStats.Tests;

public class ByteFormatterTests
{
	public ByteFormatterTests()
	{
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
	}

	[Theory]
	[InlineData(0, "0 B")]
	[InlineData(512, "512 B")]
	[InlineData(1024, "1.0 KB")]
	[InlineData(1536, "1.5 KB")]
	[InlineData(1048576, "1.0 MB")]
	public void Format_UsesExpectedUnit(double bytes, string expected)
	{
		Assert.Equal(expected, ByteFormatter.Format(bytes));
	}

	[Fact]
	public void Format_AppendsSuffix()
	{
		Assert.Equal("1.0 KB/s", ByteFormatter.Format(1024, "/s"));
	}

	[Fact]
	public void Format_TreatsNegativeValuesAsZero()
	{
		Assert.Equal("0 B", ByteFormatter.Format(-5));
	}
}
