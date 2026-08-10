using System.Collections.Specialized;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SmartmailAI.ViewModels.Controls;

namespace SmartmailAI.Views.Controls;

public sealed partial class AIinterface_Control : UserControl
{
	public AIinterface_ViewModel ViewModel { get; }

	public AIinterface_Control()
	{
		ViewModel = Ioc.Default.GetRequiredService<AIinterface_ViewModel>();
		InitializeComponent();

		ViewModel.Conversation.CollectionChanged += Conversation_CollectionChanged;
	}

	private void Conversation_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || e.NewItems.Count <= 0)
			return;

		var item = e.NewItems![0];

		DispatcherQueue.TryEnqueue(() =>
		{
			ConversationList.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
		});
	}
}
