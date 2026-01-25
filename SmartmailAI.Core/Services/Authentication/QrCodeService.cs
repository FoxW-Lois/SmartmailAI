using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using SmartmailAI.Core.Contracts.Services.Authentication;

namespace SmartmailAI.Core.Services.Authentication;

public class QrCodeService : IQrCodeService
{
	public byte[] GenerateQrCode(string text)
	{
		using var generator = new QRCodeGenerator();
		using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
		using var qr = new PngByteQRCode(data);
		return qr.GetGraphic(20);
	}

	public BitmapImage CreateBitmapImage(byte[] imageBytes)
	{
		using var stream = new MemoryStream(imageBytes);

		var bitmap = new BitmapImage();
		bitmap.SetSource(stream.AsRandomAccessStream());

		return bitmap;
	}
}
