using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.PriceDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IPriceRuleService
{
    Task<PagedResult<PriceRuleResponseDto>> GetByRoomTypeAsync(int roomTypeId, PagedRequest request, CancellationToken ct = default);

    Task<PagedResult<PriceRuleResponseDto>> GetRulesForPeriodAsync(PeriodRulesRequestDto dto, CancellationToken ct = default);

    Task<PriceRuleResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(CreatePriceRuleDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdatePriceRuleDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<PriceCalculationResponseDto> CalculatePriceAsync(PriceCalculationRequestDto dto, CancellationToken ct = default);
}