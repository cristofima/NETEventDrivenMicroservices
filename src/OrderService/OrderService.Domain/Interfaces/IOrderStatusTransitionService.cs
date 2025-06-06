using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Interfaces;

public interface IOrderStatusTransitionService
{
    void ChangeStatus(Order order, OrderStatus newStatus, DateTimeOffset eventDate, string reason = null);
}