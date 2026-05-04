// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

using HMS.API.DTOs.Report;

namespace HMS.API.Services
{
    public interface IReportService
    {
        Task<OccupancyReportDto> GetOccupancyAsync(string period, int? hotelId = null);
        Task<RevenueReportDto> GetRevenueAsync(string period, int? hotelId = null);
        Task<DemographicsReportDto> GetDemographicsAsync(int? hotelId = null);
        Task<(byte[] Data, string ContentType, string FileName)> ExportAsync(ExportRequestDto request);
    }
}
