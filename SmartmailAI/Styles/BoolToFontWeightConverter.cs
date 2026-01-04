using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;

namespace SmartmailAI.Styles;

public partial class BoolToFontWeightConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is bool isRead)
		{
			return isRead ? FontWeights.Normal : FontWeights.Bold;
		}

		return FontWeights.Normal;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
