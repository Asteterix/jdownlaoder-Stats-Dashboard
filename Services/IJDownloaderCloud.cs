using My.JDownloader.Api;
using My.JDownloader.Api.Models.Devices;
using My.JDownloader.Api.Models.DownloadsV2.Request;
using My.JDownloader.Api.Models.DownloadsV2.Response;
using My.JDownloader.Api.Models.LinkgrabberV2.Request;

namespace jdownloader_Stats.Services;

public interface IJDownloaderCloud
{
	bool Connect(string email, string password);

	IEnumerable<Device> GetDevices();

	IDeviceCloudSession? GetDeviceSession(Device device);
}

public interface IDeviceCloudSession
{
	long GetSpeedInBps();

	IEnumerable<DownloadLinkResponse> QueryLinks(LinkQueryRequest request);

	bool AddLinks(string url);
}

public sealed class JDownloaderCloud : IJDownloaderCloud
{
	private readonly JDownloaderHandler _handler;

	public JDownloaderCloud(string appKey)
	{
		_handler = new JDownloaderHandler(appKey);
	}

	public bool Connect(string email, string password) => _handler.Connect(email, password);

	public IEnumerable<Device> GetDevices() => _handler.GetDevices();

	public IDeviceCloudSession? GetDeviceSession(Device device)
	{
		var deviceHandler = _handler.GetDeviceHandler(device, useJdownloaderApi: true);
		return deviceHandler is null ? null : new DeviceCloudSession(deviceHandler);
	}

	private sealed class DeviceCloudSession(DeviceHandler device) : IDeviceCloudSession
	{
		public long GetSpeedInBps() => device.DownloadController.GetSpeedInBps();

		public IEnumerable<DownloadLinkResponse> QueryLinks(LinkQueryRequest request) =>
			device.DownloadsV2.QueryLinks(request) ?? [];

		public bool AddLinks(string url) =>
			device.LinkgrabberV2.AddLinks(new AddLinkRequest { Links = url, AutoStart = false });
	}
}
