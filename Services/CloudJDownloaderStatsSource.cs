using jdownloader_Stats.Models;
using My.JDownloader.Api;
using My.JDownloader.Api.Models.Devices;
using My.JDownloader.Api.Models.DownloadsV2.Request;
using My.JDownloader.Api.Models.DownloadsV2.Response;
using My.JDownloader.Api.Namespaces;

namespace jdownloader_Stats.Services;

public sealed class CloudJDownloaderStatsSource : IDownloadStatsSource
{
	private const string AppKey = "http://git.io/vmcsk";

	private readonly string _email;
	private readonly string _password;
	private readonly JDownloaderHandler _handler;
	private DeviceHandler? _device;
	private LinkStats _linkStats = new(0, 0, 0, 0, 0);
	private Task? _linkStatsRefresh;
	private bool _linkStatsLoaded;
	private DateTimeOffset _nextLinkStatsRefresh = DateTimeOffset.MinValue;
	private static readonly TimeSpan LinkStatsInterval = TimeSpan.FromSeconds(30);

	public CloudJDownloaderStatsSource(string email, string password)
	{
		_email = email;
		_password = password;
		_handler = new JDownloaderHandler(AppKey);
	}

	public Task ConnectAsync(CancellationToken cancellationToken)
	{
		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!_handler.Connect(_email, _password))
			{
				throw new InvalidOperationException("Could not connect to My.JDownloader.");
			}

			Device device = _handler.GetDevices().FirstOrDefault()
				?? throw new InvalidOperationException("No device is connected to the My.JDownloader account.");
			_device = _handler.GetDeviceHandler(device, useJdownloaderApi: true)
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

			long speed = device.DownloadController.GetSpeedInBps();

			if (DateTimeOffset.Now >= _nextLinkStatsRefresh && (_linkStatsRefresh is null || _linkStatsRefresh.IsCompleted))
			{
				_nextLinkStatsRefresh = DateTimeOffset.Now + LinkStatsInterval;
				_linkStatsRefresh = Task.Run(() =>
				{
					_linkStats = QueryLinkStats(device);
					_linkStatsLoaded = true;
				});
			}

			return new DownloadSnapshot(
				DateTimeOffset.Now,
				speed,
				_linkStats.ActiveDownloads,
				_linkStats.QueuedLinks,
				_linkStats.BytesLoaded,
				_linkStats.BytesTotal,
				_linkStatsLoaded,
				_linkStats.RemainingBytes);
		}, cancellationToken);
	}

	private static LinkStats QueryLinkStats(DeviceHandler device)
	{
		var links = new List<DownloadLinkResponse>();
		for (int startAt = 0; ; startAt += 100)
		{
			var page = device.DownloadsV2
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
	}
}
