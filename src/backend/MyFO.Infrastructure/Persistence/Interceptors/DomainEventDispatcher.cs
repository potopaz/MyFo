using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyFO.Application.Common.Events;
using MyFO.Domain.Common;

namespace MyFO.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events when SaveChanges is called.
///
/// How it works:
/// 1. Before saving, it collects all domain events from all tracked entities
/// 2. It clears the events from the entities (so they don't fire again)
/// 3. It publishes each event via MediatR
/// 4. The actual save happens after all events are processed
///
/// Because this runs BEFORE the commit, all event handlers execute within
/// the SAME database transaction. If any handler fails, everything rolls back.
///
/// Example flow:
///   CreateMovementHandler saves a Movement with a CreditCard payment
///   → Movement entity has a MovementCreatedEvent
///   → This interceptor finds it and publishes it
///   → OnMovementCreated_RegisterCreditCardPurchase handler runs
///   → Creates CreditCardPurchase + Installments
///   → Everything commits together (or rolls back together)
/// </summary>
public class DomainEventDispatcher : SaveChangesInterceptor
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Collect all domain events from all tracked entities
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        // Gather all events
        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events from entities so they won't fire again on retry
        entities.ForEach(e => e.ClearDomainEvents());

        // Publish each event wrapped in DomainEventNotification<T>.
        // Our domain events only implement IDomainEvent (no MediatR dependency),
        // so we wrap them in a generic INotification that MediatR can publish.
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;
            await _mediator.Publish(notification, cancellationToken);
        }
    }
}
