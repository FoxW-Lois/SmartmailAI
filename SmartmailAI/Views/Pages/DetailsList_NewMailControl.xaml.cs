using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_NewMailControl : UserControl
{
	public DetailsList_NewMailViewModel ViewModel { get; }

	public DetailsList_NewMailControl()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_NewMailViewModel>();
		InitializeComponent();
	}

	// RichEditBox n'expose pas de binding natif, passe par l'événement
	private void OnBodyChanged(object sender, RoutedEventArgs e)
	{
		BodyEditor.Document.GetText(TextGetOptions.UseCrlf, out var text);
		ViewModel.Body = text;
	}

	private void OnRemoveAttachmentClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is MailAttachment attachment)
			ViewModel.RemoveAttachmentCommand.Execute(attachment);
	}
}
