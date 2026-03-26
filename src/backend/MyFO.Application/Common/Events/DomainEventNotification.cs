using MediatR;
using MyFO.Domain.Common;

namespace MyFO.Application.Common.Events;

/// <summary>
/// Wraps a domain event (which has no MediatR dependency) into
/// an INotification that MediatR can publish.
///
/// This is the bridge between the Domain layer (no external dependencies)
/// and the Application layer (which uses MediatR).
/// </summary>
public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
