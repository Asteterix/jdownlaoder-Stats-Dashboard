using jdownloader_Stats.Models;

namespace jdownloader_Stats.Services;

public interface IDownloadStatsSource : IDisposable
{
	Task<DownloadSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
