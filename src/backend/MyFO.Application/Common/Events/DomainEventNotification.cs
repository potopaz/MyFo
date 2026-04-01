using MyFO.Application.Common.Mediator;
using MyFO.Domain.Common;

namespace MyFO.Application.Common.Events;

public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
