namespace Store.Common;

public sealed record OrderDto(Guid Id, Guid CustomerId, string Status, decimal Total, DateTimeOffset PlacedAt);
