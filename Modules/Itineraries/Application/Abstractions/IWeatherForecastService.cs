using Glinter.Modules.Itineraries.Application.Dtos;

namespace Glinter.Modules.Itineraries.Application.Abstractions;

public interface IWeatherForecastService
{
    Task<WeatherForecastResponse> GetForecastAsync(
        WeatherForecastRequest request,
        CancellationToken cancellationToken = default);
}
