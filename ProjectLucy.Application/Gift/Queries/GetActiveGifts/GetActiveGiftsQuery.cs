using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.GiftDtos;

namespace ProjectLucy.Application.Gift.Queries.GetActiveGifts;

public class GetActiveGiftsQuery : IRequest<Result<IReadOnlyList<GiftCatalogDto>>>
{
}
