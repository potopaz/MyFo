namespace MyFO.Application.Common.Interfaces;

public interface IUserQueryService
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);
}
