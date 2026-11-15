using jdownloader_Stats.Services;
using Xunit;

namespace JDownloaderStats.Tests;

public class SpeedHistoryTests
{
	[Fact]
	public void ToArray_ReturnsValuesInInsertionOrder_WhenNotFull()
	{
		var history = new SpeedHistory(5);
		history.Add(1);
		history.Add(2);

		Assert.Equal([1L, 2L], history.ToArray());
	}

	[Fact]
	public void Add_OverwritesOldestValues_WhenFull()
	{
		var history = new SpeedHistory(3);
		history.Add(1);
		history.Add(2);
		history.Add(3);
		history.Add(4);

		Assert.Equal(3, history.Count);
		Assert.Equal([2L, 3L, 4L], history.ToArray());
	}

	[Fact]
	public void Constructor_Throws_WhenCapacityIsNotPositive()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedHistory(0));
	}
}
