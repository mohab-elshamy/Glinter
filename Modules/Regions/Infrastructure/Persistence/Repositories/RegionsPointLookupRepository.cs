using System.Data;
using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

public class RegionsPointLookupRepository(RegionsDbContext db) : IRegionsPointLookupRepository
{
    public async Task<RegionHierarchyGidsDto?> GetHierarchyByPointAsync(double lat, double lon, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        var openedConnection = connection.State != ConnectionState.Open;

        if (openedConnection)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                WITH point AS (
                    SELECT ST_SetSRID(ST_MakePoint(@lon, @lat), 4326) AS geom
                )
                SELECT
                    (SELECT gid FROM adm0, point WHERE ST_Covers(boundary_geom, point.geom) LIMIT 1) AS adm0_gid,
                    (SELECT gid FROM adm1, point WHERE ST_Covers(boundary_geom, point.geom) LIMIT 1) AS adm1_gid,
                    (SELECT gid FROM adm2, point WHERE ST_Covers(boundary_geom, point.geom) LIMIT 1) AS adm2_gid,
                    (SELECT gid FROM adm3, point WHERE ST_Covers(boundary_geom, point.geom) LIMIT 1) AS adm3_gid;
                """;

            var latParameter = command.CreateParameter();
            latParameter.ParameterName = "@lat";
            latParameter.Value = lat;
            command.Parameters.Add(latParameter);

            var lonParameter = command.CreateParameter();
            lonParameter.ParameterName = "@lon";
            lonParameter.Value = lon;
            command.Parameters.Add(lonParameter);

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            var result = new RegionHierarchyGidsDto
            {
                Adm0Gid = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                Adm1Gid = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Adm2Gid = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Adm3Gid = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            };

            return result.Adm0Gid is null &&
                   result.Adm1Gid is null &&
                   result.Adm2Gid is null &&
                   result.Adm3Gid is null
                ? null
                : result;
        }
        finally
        {
            if (openedConnection)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
