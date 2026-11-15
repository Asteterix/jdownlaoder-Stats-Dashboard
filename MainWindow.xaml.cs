using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Interop;
using System.Windows.Threading;
using jdownloader_Stats.Models;
using jdownloader_Stats.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace jdownloader_Stats
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly SpeedHistory _history = new(120);
				private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
				private readonly DispatcherTimer _linkNotificationTimer = new() { Interval = TimeSpan.FromSeconds(3) };
				private string? _lastSentLink;
				private IDownloadStatsSource? _source;
				private Polyline? _graphLine;
				private bool _polling;
				private AppSettings _settings = SettingsStore.Load();
				private CloudJDownloaderStatsSource? _linkSender;
				private const int WmClipboardUpdate = 0x031D;

				public MainWindow()
		{
			InitializeComponent();
						ModeText.Text = $"Cloud: {_settings.CloudEmail}";
						_timer.Tick += async (_, _) => await PollAsync();
						_linkNotificationTimer.Tick += (_, _) =>
						{
							_linkNotificationTimer.Stop();
							LinkNotificationBorder.Visibility = Visibility.Collapsed;
						};
						GraphCanvas.SizeChanged += (_, _) => RedrawGraph();
						SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(ClipboardWndProc);
						Closed += (_, _) =>
						{
							if (ClipboardCheckBox.IsChecked == true)
							{
								RemoveClipboardFormatListener(new WindowInteropHelper(this).Handle);
							}
							Disconnect();
						};
					}

					private async void ConnectButton_Click(object sender, RoutedEventArgs e)
					{
						if (_timer.IsEnabled)
						{
							Disconnect();
							return;
						}

						if (string.IsNullOrWhiteSpace(_settings.CloudEmail) || string.IsNullOrWhiteSpace(_settings.ProtectedCloudPassword))
						{
							StatusText.Text = "Please open Setup first";
							return;
						}

						CloudJDownloaderStatsSource cloud;
						try
						{
							cloud = new CloudJDownloaderStatsSource(_settings.CloudEmail, SecretProtector.Unprotect(_settings.ProtectedCloudPassword));
							StatusText.Text = "Connecting ...";
							await cloud.ConnectAsync(CancellationToken.None);
						}
						catch (Exception ex)
						{
							StatusText.Text = DescribeError(ex);
							return;
						}
						_source = cloud;
						_linkSender = cloud;
						ConnectButton.Content = "Disconnect";
						StatusText.Text = "Connecting ...";
						_timer.Start();
						await PollAsync();
					}

					private void SetupButton_Click(object sender, RoutedEventArgs e)
					{
						var dialog = new SetupWindow(_settings) { Owner = this };
						if (dialog.ShowDialog() == true && dialog.Result is { } result)
						{
							_settings = result;
							try
							{
								SettingsStore.Save(_settings);
							}
							catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
							{
								StatusText.Text = $"Could not save settings: {ex.Message}";
							}
							ModeText.Text = $"Cloud: {_settings.CloudEmail}";
						}
					}

					private void TopmostCheckBox_Click(object sender, RoutedEventArgs e)
					{
						var onTop = TopmostCheckBox.IsChecked == true;
						Topmost = onTop;
						WindowStyle = onTop ? WindowStyle.None : WindowStyle.SingleBorderWindow;
					}

					private void ClipboardCheckBox_Click(object sender, RoutedEventArgs e)
					{
						var handle = new WindowInteropHelper(this).Handle;
						if (ClipboardCheckBox.IsChecked == true)
						{
							AddClipboardFormatListener(handle);
							StatusText.Text = "Clipboard monitoring on";
						}
						else
						{
							RemoveClipboardFormatListener(handle);
						}
					}

					private IntPtr ClipboardWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
					{
						if (msg == WmClipboardUpdate)
						{
							_ = SendClipboardLinkAsync();
						}

						return IntPtr.Zero;
					}

					private void ShowLinkNotification()
					{
						_linkNotificationTimer.Stop();
						LinkNotificationBorder.Visibility = Visibility.Visible;
						_linkNotificationTimer.Start();
					}

					private async Task SendClipboardLinkAsync()
					{
						string? text;
						try
						{
							text = Clipboard.GetText();
						}
						catch (COMException)
						{
							return;
						}

						if (!ClipboardLinkDetector.TryGetHttpsUrl(text, out var url) || url is null)
						{
							return;
						}

						if (string.Equals(url, _lastSentLink, StringComparison.Ordinal))
						{
							return;
						}

						if (_linkSender is null)
						{
							StatusText.Text = "Not connected - link not sent";
							return;
						}

						try
						{
							var accepted = await _linkSender.SendLinkAsync(url, CancellationToken.None);
							StatusText.Text = accepted ? $"Link sent: {url}" : "JDownloader rejected the link";
							if (accepted)
							{
								_lastSentLink = url;
								ShowLinkNotification();
							}
						}
						catch (Exception ex)
						{
							StatusText.Text = DescribeError(ex);
						}
					}

					[DllImport("user32.dll", SetLastError = true)]
					[return: MarshalAs(UnmanagedType.Bool)]
					private static extern bool AddClipboardFormatListener(IntPtr hwnd);

					[DllImport("user32.dll", SetLastError = true)]
					[return: MarshalAs(UnmanagedType.Bool)]
					private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

					private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
					{
						if (WindowStyle == WindowStyle.None && e.ButtonState == MouseButtonState.Pressed)
						{
							DragMove();
						}
					}

					private void Disconnect(string status = "Not connected")
					{
						_timer.Stop();
						_source?.Dispose();
						_source = null;
						_linkSender = null;
						ConnectButton.Content = "Connect";
						StatusText.Text = status;
					}

					private async Task PollAsync()
					{
						if (_source is null || _polling)
						{
							return;
						}

						_polling = true;
						try
						{
							var snapshot = await _source.GetSnapshotAsync(CancellationToken.None);
							_history.Add(snapshot.BytesPerSecond);
							Render(snapshot);
							StatusText.Text = $"Updated {snapshot.Timestamp:HH:mm:ss}";
						}
						catch (OperationCanceledException)
						{
						}
						catch (Exception ex)
						{
							Disconnect(DescribeError(ex));
						}
						finally
						{
							_polling = false;
						}
					}

					private static string DescribeError(Exception ex) => ex switch
					{
						HttpRequestException { StatusCode: { } code } => $"HTTP {(int)code} ({code}): {ex.Message}",
						HttpRequestException => $"HTTP error: {ex.Message}",
						TaskCanceledException => "Cancelled",
						OperationCanceledException => "Cancelled",
						TimeoutException => "Timeout contacting My.JDownloader",
						_ => $"{ex.GetType().Name}: {ex.Message}"
					};

					private void Render(DownloadSnapshot snapshot)
					{
						SpeedText.Text = ByteFormatter.Format(snapshot.BytesPerSecond, "/s");
						ActiveText.Text = snapshot.LinksLoaded ? $"Active: {snapshot.ActiveDownloads}" : "Active: loading";
						QueuedText.Text = snapshot.LinksLoaded ? $"Queued: {snapshot.QueuedLinks}" : "Queued: loading";
						PercentText.Text = $"{snapshot.Percent:0.0} %";
						EtaText.Text = snapshot.LinksLoaded ? FormatEta(snapshot.Eta) : "ETA: loading";
						ProgressBar.Value = snapshot.Percent;
						RedrawGraph();
					}

					private static string FormatEta(TimeSpan? eta)
					{
						if (eta is not { } value)
						{
							return "ETA: --";
						}

						return value.TotalHours >= 1
							? $"ETA: {(int)value.TotalHours}h {value.Minutes:00}m"
							: $"ETA: {value.Minutes}m {value.Seconds:00}s";
					}

					private void RedrawGraph()
					{
						var values = _history.ToArray();
						if (values.Count < 2 || GraphCanvas.ActualWidth <= 0 || GraphCanvas.ActualHeight <= 0)
						{
							return;
						}

						_graphLine ??= new Polyline { Stroke = Brushes.DeepSkyBlue, StrokeThickness = 2 };
						if (!GraphCanvas.Children.Contains(_graphLine))
						{
							GraphCanvas.Children.Add(_graphLine);
						}

						var max = Math.Max(1, values.Max());
						var width = GraphCanvas.ActualWidth;
						var height = GraphCanvas.ActualHeight;
						_graphLine.Points = new PointCollection(values.Select((value, index) =>
							new Point(width * index / (values.Count - 1), height - height * value / max)));
					}

	}
}