using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace SmartmailAI.Core.Models.AI;

public class AIMessage
{
	public string Content { get; set; } = "";

	public bool IsUser { get; set; }

	public HorizontalAlignment Alignment => IsUser
		? HorizontalAlignment.Right
		: HorizontalAlignment.Left;

	public Microsoft.UI.Xaml.Media.Brush BubbleBrush => IsUser
		? new SolidColorBrush(Colors.DodgerBlue)
		: (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"];
}
