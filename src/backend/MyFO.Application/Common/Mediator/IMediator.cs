namespace MyFO.Application.Common.Mediator;

/// <summary>
/// Placeholder return type for void request handlers.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}

/// <summary>Request that returns TResponse.</summary>
public interface IRequest<TResponse> { }

/// <summary>Request that returns nothing (void). Internally maps to IRequest&lt;Unit&gt;.</summary>
public interface IRequest : IRequest<Unit> { }

/// <summary>Handler for IRequest&lt;TResponse&gt;.</summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Handler for IRequest (void). Handle returns Task.</summary>
public interface IRequestHandler<TRequest>
    where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}

public interface INotification { }

public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish(object notification, CancellationToken cancellationToken = default);
}
