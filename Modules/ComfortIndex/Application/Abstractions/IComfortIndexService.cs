using Glinter.Modules.ComfortIndex.Application.Dtos;

namespace Glinter.Modules.ComfortIndex.Application.Abstractions;

public interface IComfortIndexService
{
    Task<ComfortIndexResponseDto> GetAsync(
        ComfortIndexRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComfortIndexResponseDto>> GetBatchAsync(
        IReadOnlyList<ComfortIndexRequest> requests,
        CancellationToken cancellationToken = default);
}
