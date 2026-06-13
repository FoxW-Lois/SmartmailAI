using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_NewMailControl : UserControl
{
	public DetailsList_NewMailViewModel ViewModel { get; }

	public DetailsList_NewMailControl()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_NewMailViewModel>();
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
