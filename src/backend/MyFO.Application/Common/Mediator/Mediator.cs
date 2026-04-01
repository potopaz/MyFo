using Microsoft.Extensions.DependencyInjection;

namespace MyFO.Application.Common.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _sp;

    public Mediator(IServiceProvider sp) => _sp = sp;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Try two-param handler first: IRequestHandler<TRequest, TResponse>
        var twoParamType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var twoParamHandler = _sp.GetService(twoParamType);
        if (twoParamHandler is not null)
        {
            dynamic h = twoParamHandler;
            return h.Handle((dynamic)request, cancellationToken);
        }

        // Fall back to one-param (void) handler: IRequestHandler<TRequest>
        // Only applicable when TResponse is Unit (i.e., request implements IRequest)
        if (typeof(TResponse) == typeof(Unit))
        {
            var oneParamType = typeof(IRequestHandler<>).MakeGenericType(request.GetType());
            var oneParamHandler = _sp.GetService(oneParamType);
            if (oneParamHandler is not null)
            {
                dynamic h = oneParamHandler;
                Task task = h.Handle((dynamic)request, cancellationToken);
                return task.ContinueWith(_ => (TResponse)(object)Unit.Value, cancellationToken);
            }
        }

        throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var handlers = _sp.GetServices(handlerType);
        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            dynamic h = handler;
            await h.Handle((dynamic)notification, cancellationToken);
        }
    }
}
