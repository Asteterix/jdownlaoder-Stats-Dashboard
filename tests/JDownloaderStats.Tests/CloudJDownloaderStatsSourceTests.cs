using System.IO;
using jdownloader_Stats.Models;
using jdownloader_Stats.Services;
using JDownloaderStats.Tests.Fakes;
using My.JDownloader.Api.Models.Devices;
using My.JDownloader.Api.Models.DownloadsV2.Response;
using Xunit;

namespace JDownloaderStats.Tests;

public class CloudJDownloaderStatsSourceTests
{
	private static DownloadLinkResponse Link(bool running = false, bool enabled = true, bool finished = false, long loaded = 0, long total = 0) =>
		new()
		{
			Running = running,
			Enabled = enabled,
			Finished = finished,
			BytesLoaded = loaded,
			BytesTotal = total,
		};

	private static (CloudJDownloaderStatsSource Source, FakeJDownloaderCloud Cloud, FakeDeviceSession Session) CreateConnected()
	{
		var session = new FakeDeviceSession();
		var cloud = new FakeJDownloaderCloud { Session = session };
		cloud.Devices.Add(new Device { Id = "dev1", Name = "Home" });
		var source = new CloudJDownloaderStatsSource("user@example.com", "secret", cloud);
		return (source, cloud, session);
	}

	private static async Task<DownloadSnapshot> SnapshotWhenLinksLoadedAsync(CloudJDownloaderStatsSource source)
	{
		DownloadSnapshot snapshot;
		do
		{
			snapshot = await source.GetSnapshotAsync(CancellationToken.None);
			await Task.Delay(10);
		}
		while (!snapshot.LinksLoaded);
		return await source.GetSnapshotAsync(CancellationToken.None);
	}

	[Fact]
	public async Task ConnectAsync_PassesCredentialsToCloud()
	{
		var (source, cloud, _) = CreateConnected();

		await source.ConnectAsync(CancellationToken.None);

		Assert.Equal("user@example.com", cloud.LastEmail);
		Assert.Equal("secret", cloud.LastPassword);
	}

	[Fact]
	public async Task SendLinkAsync_ForwardsUrlToSession()
	{
		var (source, _, session) = CreateConnected();
		await source.ConnectAsync(CancellationToken.None);

		var result = await source.SendLinkAsync("https://example.com/file.zip", CancellationToken.None);

		Assert.True(result);
		Assert.Equal(["https://example.com/file.zip"], session.AddedLinks);
	}

	[Fact]
	public async Task SendLinkAsync_ReturnsFalse_WhenSessionRejectsLink()
	{
		var (source, _, session) = CreateConnected();
		session.AddLinksResult = false;
		await source.ConnectAsync(CancellationToken.None);

		var result = await source.SendLinkAsync("https://example.com/file.zip", CancellationToken.None);

		Assert.False(result);
	}

	[Fact]
	public async Task SendLinkAsync_Throws_WhenNotConnected()
	{
		var (source, _, session) = CreateConnected();

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			source.SendLinkAsync("https://example.com/file.zip", CancellationToken.None));
		Assert.Empty(session.AddedLinks);
	}

	[Fact]
	public async Task ConnectAsync_Throws_WhenCloudRejectsLogin()
	{
		var (source, cloud, _) = CreateConnected();
		cloud.ConnectResult = false;

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => source.ConnectAsync(CancellationToken.None));

		Assert.Equal("Could not connect to My.JDownloader.", ex.Message);
	}

	[Fact]
	public async Task ConnectAsync_Throws_WhenAccountHasNoDevice()
	{
		var (source, cloud, _) = CreateConnected();
		cloud.Devices.Clear();

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => source.ConnectAsync(CancellationToken.None));

		Assert.Equal("No device is connected to the My.JDownloader account.", ex.Message);
	}

	[Fact]
	public async Task ConnectAsync_Throws_WhenDeviceSessionCannotBeCreated()
	{
		var (source, cloud, _) = CreateConnected();
		cloud.Session = null;

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => source.ConnectAsync(CancellationToken.None));

		Assert.Equal("Could not create a handler for the selected device.", ex.Message);
	}

	[Fact]
	public async Task GetSnapshotAsync_Throws_WhenNotConnected()
	{
		var (source, _, _) = CreateConnected();

		await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetSnapshotAsync(CancellationToken.None));
	}

	[Fact]
	public async Task GetSnapshotAsync_ReturnsSpeedFromDevice()
	{
		var (source, _, session) = CreateConnected();
		session.Speed = 4096;
		await source.ConnectAsync(CancellationToken.None);

		var snapshot = await source.GetSnapshotAsync(CancellationToken.None);

		Assert.Equal(4096, snapshot.BytesPerSecond);
	}

	[Fact]
	public async Task GetSnapshotAsync_SumsRunningAndQueuedLinks()
	{
		var (source, _, session) = CreateConnected();
		session.Pages.Add(
		[
			Link(running: true, loaded: 300, total: 1000),
			Link(enabled: true, finished: false, loaded: 0, total: 500),
			Link(enabled: false, loaded: 0, total: 700),
			Link(enabled: true, finished: true, loaded: 900, total: 900),
		]);
		await source.ConnectAsync(CancellationToken.None);

		var snapshot = await SnapshotWhenLinksLoadedAsync(source);

		Assert.Equal(1, snapshot.ActiveDownloads);
		Assert.Equal(1, snapshot.QueuedLinks);
		Assert.Equal(300, snapshot.BytesLoaded);
		Assert.Equal(1000, snapshot.BytesTotal);
		Assert.Equal(1200, snapshot.RemainingBytes);
	}

	[Fact]
	public async Task GetSnapshotAsync_ReadsEveryPage_WhenFirstPageIsFull()
	{
		var (source, _, session) = CreateConnected();
		session.Pages.Add(Enumerable.Range(0, 100).Select(_ => Link(enabled: true, total: 10)).ToList());
		session.Pages.Add([Link(enabled: true, total: 10)]);
		await source.ConnectAsync(CancellationToken.None);

		var snapshot = await SnapshotWhenLinksLoadedAsync(source);

		Assert.Equal([0, 100], session.RequestedStartAt);
		Assert.Equal(101, snapshot.QueuedLinks);
	}

	[Fact]
	public async Task GetSnapshotAsync_ReportsLinksNotLoaded_UntilFirstRefreshCompletes()
	{
		var (source, _, session) = CreateConnected();
		await source.ConnectAsync(CancellationToken.None);

		var snapshot = await source.GetSnapshotAsync(CancellationToken.None);

		Assert.False(snapshot.LinksLoaded);
	}

	[Fact]
	public async Task GetSnapshotAsync_ThrowsOperationCanceled_WhenCancelled()
	{
		var (source, _, _) = CreateConnected();
		await source.ConnectAsync(CancellationToken.None);
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetSnapshotAsync(cts.Token));
	}

	[Fact]
	public async Task GetSnapshotAsync_SurfacesLinkStatsFailure()
	{
		var (source, _, session) = CreateConnected();
		session.QueryException = new IOException("boom");
		await source.ConnectAsync(CancellationToken.None);

		var ex = await SnapshotFailureAsync(source);

		Assert.IsType<IOException>(ex.InnerException);
	}

	[Fact]
	public async Task SendLinkAsync_Throws_AfterDispose()
	{
		var (source, _, session) = CreateConnected();
		await source.ConnectAsync(CancellationToken.None);
		source.Dispose();

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			source.SendLinkAsync("https://example.com/file.zip", CancellationToken.None));
		Assert.Empty(session.AddedLinks);
	}

	[Fact]
	public async Task GetSnapshotAsync_Throws_AfterDispose()
	{
		var (source, _, _) = CreateConnected();
		await source.ConnectAsync(CancellationToken.None);
		source.Dispose();

		await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetSnapshotAsync(CancellationToken.None));
	}

	private static async Task<InvalidOperationException> SnapshotFailureAsync(CloudJDownloaderStatsSource source)
	{
		for (var attempt = 0; attempt < 200; attempt++)
		{
			try
			{
				await source.GetSnapshotAsync(CancellationToken.None);
			}
			catch (InvalidOperationException ex) when (ex.InnerException is not null)
			{
				return ex;
			}

			await Task.Delay(10);
		}

		throw new TimeoutException("Link stats failure was not surfaced.");
	}
}
