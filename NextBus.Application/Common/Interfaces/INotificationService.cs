using System.Threading.Tasks;
using NextBus.Domain.Entities;

namespace NextBus.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(
            string userId, 
            string title, 
            string message, 
            string type = "Booking", 
            string? actionUrl = null);
    }
}
