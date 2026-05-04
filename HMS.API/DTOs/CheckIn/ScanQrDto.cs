// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.CheckIn
{
    public class ScanQrDto
    {
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
