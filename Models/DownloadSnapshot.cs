namespace jdownloader_Stats.Models;

public sealed record DownloadSnapshot(
	DateTimeOffset Timestamp,
	long BytesPerSecond,
	int ActiveDownloads,
	int QueuedLinks,
	long BytesLoaded,
	long BytesTotal,
	bool LinksLoaded = true,
	long RemainingBytes = 0)
{
	public double Percent => BytesTotal <= 0
		? 0
		: Math.Clamp(100.0 * BytesLoaded / BytesTotal, 0, 100);

	public TimeSpan? Eta => BytesPerSecond > 0
		? TimeSpan.FromSeconds(RemainingBytes / (double)BytesPerSecond)
		: null;
}
