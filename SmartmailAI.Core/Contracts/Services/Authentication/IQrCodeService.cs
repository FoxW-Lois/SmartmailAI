using Microsoft.UI.Xaml.Media.Imaging;

namespace SmartmailAI.Core.Contracts.Services.Authentication;

public interface IQrCodeService
{
	byte[] GenerateQrCode(string otpAuthUri);

	BitmapImage CreateBitmapImage(byte[] imageBytes);
}
