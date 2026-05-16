
using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.CheckIn
{
    public class ScanQrDto
    {
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
