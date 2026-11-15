using jdownloader_Stats.Services;
using My.JDownloader.Api.Models.Devices;
using My.JDownloader.Api.Models.DownloadsV2.Request;
using My.JDownloader.Api.Models.DownloadsV2.Response;

namespace JDownloaderStats.Tests.Fakes;

internal sealed class FakeJDownloaderCloud : IJDownloaderCloud
{
	public bool ConnectResult { get; set; } = true;

	public List<Device> Devices { get; } = [];

	public FakeDeviceSession? Session { get; set; }

	public string? LastEmail { get; private set; }

	public string? LastPassword { get; private set; }

	public bool Connect(string email, string password)
	{
		LastEmail = email;
		LastPassword = password;
		return ConnectResult;
	}

	public IEnumerable<Device> GetDevices() => Devices;

	public IDeviceCloudSession? GetDeviceSession(Device device) => Session;
}

internal sealed class FakeDeviceSession : IDeviceCloudSession
{
	public long Speed { get; set; }

	public List<List<DownloadLinkResponse>> Pages { get; } = [];

	public List<int> RequestedStartAt { get; } = [];

	public long GetSpeedInBps() => Speed;

	public IEnumerable<DownloadLinkResponse> QueryLinks(LinkQueryRequest request)
	{
		RequestedStartAt.Add(request.StartAt);
		int index = request.StartAt / 100;
		return index < Pages.Count ? Pages[index] : [];
	}
}
