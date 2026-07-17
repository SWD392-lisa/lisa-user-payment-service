using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.GiftDtos;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Gift.Queries.GetActiveGifts;

public class GetActiveGiftsQueryHandler : IRequestHandler<GetActiveGiftsQuery, Result<IReadOnlyList<GiftCatalogDto>>>
{
    private readonly IGiftCatalogRepository _giftCatalogRepo;

    public GetActiveGiftsQueryHandler(IGiftCatalogRepository giftCatalogRepo)
    {
        _giftCatalogRepo = giftCatalogRepo;
    }

    public async Task<Result<IReadOnlyList<GiftCatalogDto>>> Handle(GetActiveGiftsQuery request, CancellationToken ct)
    {
        var gifts = await _giftCatalogRepo.GetActiveAsync(ct);
        var dtos = gifts.Select(g => new GiftCatalogDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            IconUrl = g.IconUrl,
            Price = g.Price,
            Currency = g.Currency,
            IsActive = g.IsActive
        }).ToList();

        return Result<IReadOnlyList<GiftCatalogDto>>.Success(dtos, "Active gifts retrieved");
    }
}
