using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartmailAI.Views.Pages;

public sealed partial class DetailsList_StandardControl : UserControl
{
	public DetailsList_StandardControl()
	{
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
}
