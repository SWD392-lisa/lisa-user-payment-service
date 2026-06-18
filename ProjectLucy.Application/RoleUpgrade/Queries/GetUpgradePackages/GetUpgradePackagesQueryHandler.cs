using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;

namespace ProjectLucy.Application.RoleUpgrade.Queries.GetUpgradePackages;

public class GetUpgradePackagesQueryHandler : IRequestHandler<GetUpgradePackagesQuery, Result<List<RolePriceDto>>>
{
    private readonly IRolePriceRepository _rolePriceRepo;
    private readonly IUserRepository _userRepo;

    public GetUpgradePackagesQueryHandler(
        IRolePriceRepository rolePriceRepo,
        IUserRepository userRepo)
    {
        _rolePriceRepo = rolePriceRepo;
        _userRepo = userRepo;
    }

    public async Task<Result<List<RolePriceDto>>> Handle(GetUpgradePackagesQuery query, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(query.UserId, ct);
        if (user is null)
            return Result<List<RolePriceDto>>.Failure(404, "User not found");

        var prices = await _rolePriceRepo.GetActiveAsync(ct);

        var dtos = prices.Select(rp => new RolePriceDto
        {
            RolePriceId = rp.Id,
            RoleCode = rp.Role.RoleCode,
            RoleName = rp.Role.RoleName,
            Price = rp.Price,
            Currency = rp.Currency,
            Description = rp.Description,
            IsCurrentRole = rp.RoleId == user.RoleId,
        }).ToList();

        return Result<List<RolePriceDto>>.Success(dtos);
    }
}
