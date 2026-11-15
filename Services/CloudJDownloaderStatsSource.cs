using jdownloader_Stats.Models;
using My.JDownloader.Api.Models.Devices;
using My.JDownloader.Api.Models.DownloadsV2.Request;
using My.JDownloader.Api.Models.DownloadsV2.Response;

namespace jdownloader_Stats.Services;

public sealed class CloudJDownloaderStatsSource : IDownloadStatsSource
{
	private const string AppKey = "jdownlaoder-Stats-Dashboard";

	private readonly string _email;
	private readonly string _password;
	private readonly IJDownloaderCloud _cloud;
	private IDeviceCloudSession? _device;
	private readonly Lock _linkStatsLock = new();
	private LinkStats _linkStats = new(0, 0, 0, 0, 0);
	private Task? _linkStatsRefresh;
	private bool _linkStatsLoaded;
	private Exception? _linkStatsFault;
	private int _linkStatsFailures;
	private DateTimeOffset _nextLinkStatsRefresh = DateTimeOffset.MinValue;
	private static readonly TimeSpan LinkStatsInterval = TimeSpan.FromSeconds(30);
	private const int MaxConsecutiveLinkStatsFailures = 3;

	public CloudJDownloaderStatsSource(string email, string password)
		: this(email, password, new JDownloaderCloud(AppKey))
	{
	}

	public CloudJDownloaderStatsSource(string email, string password, IJDownloaderCloud cloud)
	{
		_email = email;
		_password = password;
		_cloud = cloud;
	}

	public Task ConnectAsync(CancellationToken cancellationToken)
	{
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!_cloud.Connect(_email, _password))
			{
				throw new InvalidOperationException("Could not connect to My.JDownloader.");
			}

			Device device = _cloud.GetDevices().FirstOrDefault()
				?? throw new InvalidOperationException("No device is connected to the My.JDownloader account.");
			_device = _cloud.GetDeviceSession(device)
				?? throw new InvalidOperationException("Could not create a handler for the selected device.");
		}, cancellationToken);
	}

	public Task<DownloadSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
	{
		if (_device is null)
		{
			throw new InvalidOperationException("Not connected to My.JDownloader.");
		}

		var device = _device;
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();

			long speed = device.GetSpeedInBps();

			if (DateTimeOffset.Now >= _nextLinkStatsRefresh && (_linkStatsRefresh is null || _linkStatsRefresh.IsCompleted))
			{
				_nextLinkStatsRefresh = DateTimeOffset.Now + LinkStatsInterval;
				_linkStatsRefresh = Task.Run(() => RefreshLinkStats(device));
			}

			LinkStats linkStats;
			bool linkStatsLoaded;
			int linkStatsFailures;
			Exception? linkStatsFault;
			lock (_linkStatsLock)
			{
				linkStats = _linkStats;
				linkStatsLoaded = _linkStatsLoaded;
				linkStatsFailures = _linkStatsFailures;
				linkStatsFault = _linkStatsFault;
			}

			if (linkStatsFailures >= MaxConsecutiveLinkStatsFailures)
			{
				throw new InvalidOperationException("Could not load link statistics from My.JDownloader.", linkStatsFault);
			}

			return new DownloadSnapshot(
				DateTimeOffset.Now,
				speed,
				linkStats.ActiveDownloads,
				linkStats.QueuedLinks,
				linkStats.BytesLoaded,
				linkStats.BytesTotal,
				linkStatsLoaded,
				linkStats.RemainingBytes);
		}, cancellationToken);
	}

	public Task<bool> SendLinkAsync(string url, CancellationToken cancellationToken)
	{
		if (_device is null)
		{
			throw new InvalidOperationException("Not connected to My.JDownloader.");
		}

		var device = _device;
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			return device.AddLinks(url);
		}, cancellationToken);
	}

	private void RefreshLinkStats(IDeviceCloudSession device)
	{
		try
		{
			var stats = QueryLinkStats(device);
			lock (_linkStatsLock)
			{
				_linkStats = stats;
				_linkStatsLoaded = true;
				_linkStatsFailures = 0;
			}
		}
		catch (Exception ex)
		{
			lock (_linkStatsLock)
			{
				_linkStatsFault = ex;
				_linkStatsFailures++;
			}
		}
	}

	private static LinkStats QueryLinkStats(IDeviceCloudSession device)
	{
		var links = new List<DownloadLinkResponse>();
		for (int startAt = 0; ; startAt += 100)
		{
			var page = device
				.QueryLinks(new LinkQueryRequest
				{
					MaxResults = 100,
					StartAt = startAt,
					AddedDate = false,
					Comment = false,
					Eta = false,
					ExtractionStatus = false,
					FinishedDate = false,
					Host = false,
					Password = false,
					Priority = false,
					Skipped = false,
					SaveTo = false,
					Speed = false,
					Status = false,
					Url = false
				})?
				.ToList() ?? [];
			links.AddRange(page);
			if (page.Count < 100)
			{
				break;
			}
		}

		var runningLink = links.FirstOrDefault(link => link.Running);
		return new LinkStats(
			links.Count(link => link.Running),
			links.Count(link => link.Enabled && !link.Finished && !link.Running),
			runningLink?.BytesLoaded ?? 0,
			runningLink?.BytesTotal ?? 0,
			links
				.Where(link => link.Enabled && !link.Finished)
				.Sum(link => Math.Max(0, link.BytesTotal - link.BytesLoaded)));
	}

	private sealed record LinkStats(int ActiveDownloads, int QueuedLinks, long BytesLoaded, long BytesTotal, long RemainingBytes);

	public void Dispose()
	{
		_device = null;
	}
}
