using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        // EF Core automatically tracks changes to entities retrieved from the context.
        // So, if 'order' was retrieved using GetByIdAsync from the same context instance
        // and then modified, SaveChangesAsync is enough.
        // If 'order' is a detached entity or from a different context, you might need:
        // _context.Orders.Update(order);
        // However, since GetByIdAsync is used in handlers, the entity should be tracked.
        _context.Entry(order).State = EntityState.Modified; // Explicitly mark as modified
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts if necessary
            // For example, log the error and potentially re-throw or implement a retry strategy.
            var logger = _context.GetService<ILogger<OrderRepository>>(); // Example of getting logger if needed
            logger.LogError(ex, "Concurrency conflict occurred while updating order {OrderId}.", order.Id);
            throw; // Re-throw for now
        }
    }
}