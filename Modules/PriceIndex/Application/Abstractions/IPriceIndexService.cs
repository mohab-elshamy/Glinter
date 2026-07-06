using Glinter.Modules.PriceIndex.Application.Dtos;

namespace Glinter.Modules.PriceIndex.Application.Abstractions;

public interface IPriceIndexService
{
    Task<PriceIndexResponseDto> GetAsync(
        PriceIndexRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceIndexResponseDto>> GetBatchAsync(
        IReadOnlyList<PriceIndexRequest> requests,
        CancellationToken cancellationToken = default);
}
