using System.Net.Http;
using System.Text.Json;
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
				private IDownloadStatsSource? _source;
				private Polyline? _graphLine;
				private bool _polling;
				private AppSettings _settings = SettingsStore.Load();

				public MainWindow()
		{
			InitializeComponent();
						ModeText.Text = $"Cloud: {_settings.CloudEmail}";
						_timer.Tick += async (_, _) => await PollAsync();
						GraphCanvas.SizeChanged += (_, _) => RedrawGraph();
						Closed += (_, _) => Disconnect();
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
							StatusText.Text = "Bitte zuerst Setup öffnen";
							return;
						}

						var cloud = new CloudJDownloaderStatsSource(_settings.CloudEmail, SecretProtector.Unprotect(_settings.ProtectedCloudPassword));
						try
						{
							StatusText.Text = "Verbinde ...";
							await cloud.ConnectAsync(CancellationToken.None);
						}
						catch (Exception ex)
						{
							StatusText.Text = DescribeError(ex);
							return;
						}
						_source = cloud;
						ConnectButton.Content = "Trennen";
						StatusText.Text = "Verbinde ...";
						_timer.Start();
						await PollAsync();
					}

					private void SetupButton_Click(object sender, RoutedEventArgs e)
					{
						var dialog = new SetupWindow(_settings) { Owner = this };
						if (dialog.ShowDialog() == true && dialog.Result is { } result)
						{
							_settings = result;
							SettingsStore.Save(_settings);
							ModeText.Text = $"Cloud: {_settings.CloudEmail}";
						}
					}

					private void TopmostCheckBox_Click(object sender, RoutedEventArgs e)
					{
						var onTop = TopmostCheckBox.IsChecked == true;
						Topmost = onTop;
						WindowStyle = onTop ? WindowStyle.None : WindowStyle.SingleBorderWindow;
					}

					private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
					{
						if (WindowStyle == WindowStyle.None && e.ButtonState == MouseButtonState.Pressed)
						{
							DragMove();
						}
					}

					private void Disconnect(string status = "Nicht verbunden")
					{
						_timer.Stop();
						_source?.Dispose();
						_source = null;
						ConnectButton.Content = "Verbinden";
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
							StatusText.Text = $"Aktualisiert {snapshot.Timestamp:HH:mm:ss}";
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
						HttpRequestException => $"HTTP-Fehler: {ex.Message}",
						TaskCanceledException => "Abgebrochen",
						OperationCanceledException => "Abgebrochen",
						TimeoutException => "Zeitüberschreitung bei My.JDownloader",
						_ => $"{ex.GetType().Name}: {ex.Message}"
					};

					private void Render(DownloadSnapshot snapshot)
					{
						SpeedText.Text = ByteFormatter.Format(snapshot.BytesPerSecond, "/s");
						ActiveText.Text = snapshot.LinksLoaded ? $"Aktiv: {snapshot.ActiveDownloads}" : "Aktiv: laden";
						QueuedText.Text = snapshot.LinksLoaded ? $"Warteschlange: {snapshot.QueuedLinks}" : "Warteschlange: laden";
						PercentText.Text = $"{snapshot.Percent:0.0} %";
						EtaText.Text = snapshot.LinksLoaded ? FormatEta(snapshot.Eta) : "ETA: laden";
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