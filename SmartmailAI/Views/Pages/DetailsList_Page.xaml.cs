using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_Page : Page, INotifyPropertyChanged
{
	public DetailsList_ViewModel ViewModel { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public double HalfWindowWidth => ActualWidth / 2;

	public DetailsList_Page()
	{
		ViewModel = Ioc.Default.GetRequiredService<DetailsList_ViewModel>();
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
}
