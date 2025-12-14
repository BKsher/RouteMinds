using RouteMinds.Domain.Entities;

namespace RouteMinds.Domain.Interfaces
{
    public interface IOrderRepository
    {
        // We use Task because DB operations are Async (I/O bound)
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetAllAsync();
        Task AddAsync(Order order);

        // SaveChanges is usually handled by a "Unit of Work", 
        // but for simplicity, we'll keep it simple here.
        Task SaveChangesAsync();
    }
}