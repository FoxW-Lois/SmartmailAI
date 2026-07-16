using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SmartmailAI.Core.Models.Messengers;

namespace SmartmailAI.Views.Pages;

public sealed partial class EmailList_Page : Page, INotifyPropertyChanged
{
	public EmailList_ViewModel ViewModel { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	private MailboxCategory? _currentSelectedCategory;
	private Email? _previousSelectedEmail;
	private bool isAlreadyDone = false;

	public EmailList_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<EmailList_ViewModel>();
		ViewModel.RestoreSelectionRequested += OnRestoreSelectionRequested;
		DataContext = ViewModel;
		InitializeComponent();

		// Quand reçoit une demande, ferme la fenêtre des détails de l'email actuellement ouvert
		WeakReferenceMessenger.Default.Register<RequestCloseDetailsMessage>(this, (r, m) =>
		{
			OnCloseSelectionRequested();
			_previousSelectedEmail = null;
		});
	}

	private void OnViewStateChanged(object sender, ListDetailsViewState e)
	{
		if (e == ListDetailsViewState.Both)
		{
			ViewModel.EnsureItemSelected();
		}
	}

	private void OnCategoryListDetailsSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is not MailboxCategory mailboxCategory || _currentSelectedCategory == mailboxCategory)
			return;

		if (_previousSelectedEmail is not null && isAlreadyDone && _currentSelectedCategory is not null
			&& _currentSelectedCategory.MailboxType == MailboxType.Unread)
		{
			ViewModel.MarkAsReadCommand.Execute(_previousSelectedEmail);
		}

		// Mémorise la nouvelle catégorie sélectionnée
		_currentSelectedCategory = mailboxCategory;
	}

	private void OnCloseSelectionRequested()
	{
		// Parcourt les ListDetailsView imbriqués pour trouver le bon
		var innerListDetails = FindInnerListDetailsView(ListDetailsViewControl);
		if (innerListDetails is null)
			return;

		DispatcherQueue.TryEnqueue(() =>
		{
			innerListDetails.SelectedItem = null;
		});
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

	private void OnInnerListDetailsSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_currentSelectedCategory is null)
			return;

		if (e.AddedItems.FirstOrDefault() is not Email email)
			return;

		if (_currentSelectedCategory.MailboxType is not MailboxType.Unread) // Comportement pour toutes les catégories
		{
			// Marque comme lu l'email qui vient d'être quitté
			if (_previousSelectedEmail is { IsRead: false })
			{
				ViewModel.MarkAsReadCommand.Execute(_previousSelectedEmail);
			}

			// Mémorise le nouvel email sélectionné
			_previousSelectedEmail = email;
			return;
		}

		// Comportement pour la catégorie Unread
		if (_previousSelectedEmail == email)
			return;

		if (_previousSelectedEmail is { IsRead: false } && isAlreadyDone)
		{
			ViewModel.MarkAsReadCommand.Execute(_previousSelectedEmail);
			isAlreadyDone = false;
		}
		else
		{
			isAlreadyDone = true;
		}

		_previousSelectedEmail = email;
	}
}
