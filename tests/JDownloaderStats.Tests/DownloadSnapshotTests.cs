using jdownloader_Stats.Models;
using Xunit;

namespace JDownloaderStats.Tests;

public class DownloadSnapshotTests
{
	[Fact]
	public void Percent_IsZero_WhenTotalIsZero()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, 500, 0);

		Assert.Equal(0, snapshot.Percent);
	}

	[Fact]
	public void Percent_IsZero_WhenTotalIsNegative()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, 500, -1);

		Assert.Equal(0, snapshot.Percent);
	}

	[Fact]
	public void Percent_IsProportional_WhenTotalIsPositive()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, 250, 1000);

		Assert.Equal(25.0, snapshot.Percent, precision: 6);
	}

	[Fact]
	public void Percent_IsClampedToHundred_WhenLoadedExceedsTotal()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, 2000, 1000);

		Assert.Equal(100, snapshot.Percent);
	}

	[Fact]
	public void Percent_IsClampedToZero_WhenLoadedIsNegative()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, -10, 1000);

		Assert.Equal(0, snapshot.Percent);
	}

	[Fact]
	public void Eta_IsNull_WhenSpeedIsZero()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 0, 0, 0, 0, 0, RemainingBytes: 1000);

		Assert.Null(snapshot.Eta);
	}

	[Fact]
	public void Eta_IsNull_WhenSpeedIsNegative()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, -5, 0, 0, 0, 0, RemainingBytes: 1000);

		Assert.Null(snapshot.Eta);
	}

	[Fact]
	public void Eta_IsRemainingBytesDividedBySpeed_WhenSpeedIsPositive()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 100, 0, 0, 0, 0, RemainingBytes: 1000);

		Assert.Equal(TimeSpan.FromSeconds(10), snapshot.Eta);
	}

	[Fact]
	public void Eta_IsZero_WhenNothingRemains()
	{
		var snapshot = new DownloadSnapshot(DateTimeOffset.UnixEpoch, 100, 0, 0, 0, 0, RemainingBytes: 0);

		Assert.Equal(TimeSpan.Zero, snapshot.Eta);
	}
}
