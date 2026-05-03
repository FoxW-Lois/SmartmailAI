using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_StandardControl : UserControl
{
	public DetailsList_StandardViewModel ViewModel { get; }

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
		if (d is DetailsList_StandardControl control)
		{
			control.ForegroundElement.ChangeView(0, 0, 1);
		}
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
		if (folder == null)
			return;

		await ViewModel.SaveAttachmentCommand.ExecuteAsync((DetailsListMenuItem_Email!.Guid, attachment, folder.Path));
	}
}
