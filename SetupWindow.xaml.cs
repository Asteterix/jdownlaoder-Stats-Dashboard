using System.Windows;
using jdownloader_Stats.Services;

namespace jdownloader_Stats;

public partial class SetupWindow : Window
{
	private readonly AppSettings _current;

	public AppSettings? Result { get; private set; }

	public SetupWindow(AppSettings current)
	{
		InitializeComponent();
		_current = current;
		EmailBox.Text = current.CloudEmail;
	}

	private void Save_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(EmailBox.Text) || (PasswordBox.Password.Length == 0 && string.IsNullOrEmpty(_current.ProtectedCloudPassword)))
		{
			MessageText.Text = "E-Mail und Passwort eingeben.";
			return;
		}

		var password = PasswordBox.Password.Length > 0
			? SecretProtector.Protect(PasswordBox.Password)
			: _current.ProtectedCloudPassword;

		Result = new AppSettings
		{
			CloudEmail = EmailBox.Text.Trim(),
			ProtectedCloudPassword = password
		};
		DialogResult = true;
	}
}
