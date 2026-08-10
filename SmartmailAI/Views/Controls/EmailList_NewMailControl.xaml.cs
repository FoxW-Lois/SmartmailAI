using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartmailAI.ViewModels.Controls;

namespace SmartmailAI.Views.Controls;

public sealed partial class EmailList_NewMailControl : UserControl
{
	public EmailList_NewMailViewModel ViewModel { get; }

	public EmailList_NewMailControl()
	{
		ViewModel = Ioc.Default.GetRequiredService<EmailList_NewMailViewModel>();
		InitializeComponent();

		ViewModel.PropertyChanged += ViewModel_PropertyChanged!;
	}

	// RichEditBox n'expose pas de binding natif, donc obligé de passer par l'événement
	private void OnBodyChanged(object sender, RoutedEventArgs e)
	{
		BodyEditor.Document.GetText(TextGetOptions.UseCrlf, out var text);
		ViewModel.Body = text;
	}

	private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ViewModel.Body))
			return;

		BodyEditor.Document.GetText(TextGetOptions.UseCrlf, out var currentText);

		if (currentText == ViewModel.Body)
			return;

		BodyEditor.Document.SetText(TextSetOptions.None, ViewModel.Body ?? string.Empty);
	}

	private void OnRemoveAttachmentClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is MailAttachment attachment)
			ViewModel.RemoveAttachmentCommand.Execute(attachment);
	}
}
