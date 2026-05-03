// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using QRCoder;

namespace HMS.API.Services
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateBase64(string content)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var pngByteQRCode = new PngByteQRCode(qrCodeData);
            var pngBytes = pngByteQRCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        }
    }
}
