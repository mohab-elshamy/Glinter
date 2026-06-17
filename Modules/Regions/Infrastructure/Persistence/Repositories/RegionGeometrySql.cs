using System.Data;
using Glinter.Modules.Regions.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

internal static class RegionGeometrySql
{
    internal static async Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(
        RegionsDbContext db,
        string tableName,
        int gid,
        int geometryAccuracy,
        CancellationToken ct)
        => (await GetGeometryGeoJsonByIdsAsync(db, tableName, [gid], geometryAccuracy, ct)).FirstOrDefault();

    internal static async Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(
        RegionsDbContext db,
        string tableName,
        IReadOnlyCollection<int> gids,
        int geometryAccuracy,
        CancellationToken ct)
    {
        if (gids.Count == 0 || geometryAccuracy <= 0)
        {
            return [];
        }

        var connection = db.Database.GetDbConnection();
        var openedConnection = connection.State != ConnectionState.Open;

        if (openedConnection)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await using var command = connection.CreateCommand();
            var gidParameterNames = gids
                .Select((gid, index) =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@gid{index}";
                    parameter.Value = gid;
                    command.Parameters.Add(parameter);

                    return parameter.ParameterName;
                })
                .ToArray();

            command.CommandText = geometryAccuracy >= 100
                ? $"SELECT gid, ST_AsGeoJSON(boundary_geom) FROM {tableName} WHERE gid IN ({string.Join(", ", gidParameterNames)})"
                : $"SELECT gid, ST_AsGeoJSON(ST_SimplifyPreserveTopology(boundary_geom, @tolerance)) FROM {tableName} WHERE gid IN ({string.Join(", ", gidParameterNames)})";

            if (geometryAccuracy < 100)
            {
                var toleranceParameter = command.CreateParameter();
                toleranceParameter.ParameterName = "@tolerance";
                toleranceParameter.Value = CalculateTolerance(geometryAccuracy);
                command.Parameters.Add(toleranceParameter);
            }

            var results = new List<RegionGeometryGeoJson>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new RegionGeometryGeoJson(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return results;
        }
        finally
        {
            if (openedConnection)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    private static double CalculateTolerance(int geometryAccuracy)
        => 0.01d * (100 - geometryAccuracy) / 99;
}
