using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.RoleUpgradeDtos;

namespace ProjectLucy.Application.RoleUpgrade.Queries.GetUpgradePackages;

public record GetUpgradePackagesQuery(Guid UserId) : IRequest<Result<List<RolePriceDto>>>;
