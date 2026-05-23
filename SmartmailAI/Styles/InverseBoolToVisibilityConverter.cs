using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SmartmailAI.Styles;

public partial class InverseBoolToVisibilityConverter : IValueConverter
{
	// Pour Mode=OneWay
	public object Convert(object value, Type targetType, object parameter, string language)
		=> (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

	// Pour Mode=TwoWay
	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
}
