// Student ID: S2401276
// Student Name: Mohamed Iyaadh Ahmed
// Module: Advanced Software Development (UFCF8S-30-2)

namespace HMS.API.Services
{
    public interface IOccupancyBroadcaster
    {
        Task BroadcastAsync(int hotelId);
    }
}
