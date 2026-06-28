using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_StandardControl : UserControl
{
	public DetailsList_StandardViewModel ViewModel { get; }
	private string? _lastRenderedEmail;

	public DetailsList_StandardControl()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_StandardViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public Email? DetailsListMenuItem_Email
	{
		get => GetValue(DetailsListMenuItem_EmailProperty) as Email;
		set => SetValue(DetailsListMenuItem_EmailProperty, value);
	}

	public static readonly DependencyProperty DetailsListMenuItem_EmailProperty = DependencyProperty.Register("DetailsListMenuItem_Email",
		typeof(Email), typeof(DetailsList_StandardControl), new PropertyMetadata(null, OnDetailsListMenuItemPropertyChanged));

	private static void OnDetailsListMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is DetailsList_StandardControl control && e.NewValue is Email email)
		{
			control.ViewModel.CurrentEmail = email;

			control.ForegroundElement.ChangeView(0, 0, 1);

			control.DispatcherQueue.TryEnqueue(async () =>
			{
				await control.RenderEmailAsync(email);
			});
		}
	}

	private async Task RenderEmailAsync(Email email)
	{
		if (!email.IsHtmlContent)
			return;

		if (_lastRenderedEmail == email.Content)
			return;

		_lastRenderedEmail = email.Content;

		await MailWebView.EnsureCoreWebView2Async();

		MailWebView.NavigateToString(WrapHtml(email.Content!));
	}

	private static string WrapHtml(string html)
	{
		return $@"
		<!DOCTYPE html>
		<html>
		<head>
		<meta charset='utf-8'>
		<meta name='viewport' content='width=device-width, initial-scale=1.0'>
		<style>
		body {{
			font-family: Segoe UI;
			margin: 12px;
		}}
		</style>
		</head>
		<body>
		{html}
		</body>
		</html>";
	}

	private async void OnAttachmentClick(object sender, RoutedEventArgs e)
	{
		if (sender is not Button btn || btn.Tag is not MailAttachment attachment)
			return;

		// Ouvre un explorateur de fichier pour choisir où sauvegarder la pièce jointe
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
		var picker = new FolderPicker(windowId);

		var folder = await picker.PickSingleFolderAsync();
		if (folder is null)
			return;

		await ViewModel.SaveAttachmentCommand.ExecuteAsync((DetailsListMenuItem_Email!.Guid, attachment, folder.Path));
	}
}
