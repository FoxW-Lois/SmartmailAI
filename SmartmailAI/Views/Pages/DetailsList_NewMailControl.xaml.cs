using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Streams;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_NewMailControl : UserControl
{
	public DetailsList_NewMailViewModel ViewModel { get; }

	public DetailsList_NewMailControl()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_NewMailViewModel>();
		InitializeComponent();
	}

	// RichEditBox n'expose pas de binding natif, donc obligé de passer par l'événement
	private async void OnBodyChanged(object sender, RoutedEventArgs e)
	{
		//BodyEditor.Document.GetText(TextGetOptions.UseCrlf, out var text);
		//ViewModel.Body = text;

		ViewModel.Body = await GetRtfAsync();
	}

	private void OnRemoveAttachmentClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is MailAttachment attachment)
			ViewModel.RemoveAttachmentCommand.Execute(attachment);
	}

	private async Task<string> GetRtfAsync()
	{
		using var stream = new InMemoryRandomAccessStream();

		BodyEditor.Document.SaveToStream(TextGetOptions.FormatRtf, stream);

		stream.Seek(0);

		var reader = new DataReader(stream.GetInputStreamAt(0));
		await reader.LoadAsync((uint)stream.Size);

		return reader.ReadString((uint)stream.Size);
	}
}
