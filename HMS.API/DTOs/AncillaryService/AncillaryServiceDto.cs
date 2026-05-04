// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using System.ComponentModel.DataAnnotations;

namespace HMS.API.DTOs.AncillaryService
{
    public class AncillaryServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class CreateAncillaryServiceDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(0.01, 100000)] public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateAncillaryServiceDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(0.01, 100000)] public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}
