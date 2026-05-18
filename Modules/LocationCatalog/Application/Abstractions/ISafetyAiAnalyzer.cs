using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

namespace Glinter.Modules.LocationCatalog.Application.Abstractions;

public interface ISafetyAiAnalyzer
{
    Task<SafetyAiAnalysisResult> AnalyzeAsync(
        string districtName,
        string text,
        CancellationToken cancellationToken = default);
}