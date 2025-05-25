namespace OrderService.Api.DTO;

public readonly record struct  CancelOrderRequestPayload(string? Reason = null);
public readonly record struct ShipOrderRequestPayload(string? TrackingNumber = null);