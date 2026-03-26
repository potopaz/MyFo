using Microsoft.AspNetCore.Identity;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Infrastructure.Identity;

public class UserQueryService : IUserQueryService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserQueryService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }
}
