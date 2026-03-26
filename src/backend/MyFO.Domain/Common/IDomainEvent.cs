namespace MyFO.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// The Application/Infrastructure layers will bridge this to MediatR's INotification.
/// This keeps the Domain layer free of external dependencies.
/// </summary>
public interface IDomainEvent;
