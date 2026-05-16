
namespace HMS.API.Services
{
    public interface IOccupancyBroadcaster
    {
        Task BroadcastAsync(int hotelId);
    }
}
