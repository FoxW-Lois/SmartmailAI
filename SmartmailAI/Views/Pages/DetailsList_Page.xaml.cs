using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_Page : Page, INotifyPropertyChanged
{
	public DetailsList_ViewModel ViewModel { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public double HalfWindowWidth => ActualWidth / 2;

	public DetailsList_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_ViewModel>();
		ViewModel.RestoreSelectionRequested += OnRestoreSelectionRequested;
		DataContext = ViewModel;
		InitializeComponent();
		SizeChanged += (s, e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HalfWindowWidth)));
	}

	private void OnViewStateChanged(object sender, ListDetailsViewState e)
	{
		if (e == ListDetailsViewState.Both)
		{
			ViewModel.EnsureItemSelected();
		}
	}

	private void OnRestoreSelectionRequested(Email email)
	{
		// Parcourt les ListDetailsView imbriqués pour trouver le bon
		var innerListDetails = FindInnerListDetailsView(ListDetailsViewControl);
		if (innerListDetails is null)
			return;

		DispatcherQueue.TryEnqueue(() =>
		{
			innerListDetails.SelectedItem = email;
		});
	}

	private static ListDetailsView? FindInnerListDetailsView(DependencyObject parent)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is ListDetailsView ldv)
				return ldv;

			var result = FindInnerListDetailsView(child);
			if (result is not null)
				return result;
		}
		return null;
	}

	// Marque l'email comme lu lorsqu'il est cliqué => lorsqu'on ouvre la vue de ses détails
	private void OnInnerListDetailsSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is Email email && !email.IsRead)
		{
			ViewModel.MarkAsReadCommand.Execute(email);
		}
	}
}
