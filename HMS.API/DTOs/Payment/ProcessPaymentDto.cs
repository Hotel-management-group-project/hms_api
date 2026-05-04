// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.Payment
{
    public class ProcessPaymentDto
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Amount { get; set; }

        public string Method { get; set; } = "Mock";
    }
}
