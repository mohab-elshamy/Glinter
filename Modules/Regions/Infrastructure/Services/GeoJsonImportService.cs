using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json;

namespace Glinter.Modules.Regions.Infrastructure.Services;

/// <summary>
/// Reads GeoJSON FeatureCollections and upserts adm0–adm3 records.
/// Primary property keys are the actual keys detected in the Egypt OCHA files.
/// Fallbacks cover OCHA standard casing and generic aliases.
/// </summary>
public class GeoJsonImportService(RegionsDbContext db) : IGeoJsonImportService
{
    // ─────────────────────────────────────────────────────────
    //  Public entry points
    // ─────────────────────────────────────────────────────────

    public Task<GeoJsonImportResultDto> ImportAdm0Async(Stream geojsonStream, CancellationToken ct = default)
        => ImportAsync("adm0", geojsonStream, ImportAdm0FeatureAsync, ct);

    public Task<GeoJsonImportResultDto> ImportAdm1Async(Stream geojsonStream, CancellationToken ct = default)
        => ImportAsync("adm1", geojsonStream, ImportAdm1FeatureAsync, ct);

    public Task<GeoJsonImportResultDto> ImportAdm2Async(Stream geojsonStream, CancellationToken ct = default)
        => ImportAsync("adm2", geojsonStream, ImportAdm2FeatureAsync, ct);

    public Task<GeoJsonImportResultDto> ImportAdm3Async(Stream geojsonStream, CancellationToken ct = default)
        => ImportAsync("adm3", geojsonStream, ImportAdm3FeatureAsync, ct);

    // ─────────────────────────────────────────────────────────
    //  Core import loop
    // ─────────────────────────────────────────────────────────

    private async Task<GeoJsonImportResultDto> ImportAsync(
        string layer,
        Stream geojsonStream,
        Func<IFeature, ImportCounters, CancellationToken, Task> featureHandler,
        CancellationToken ct)
    {
        var result = new GeoJsonImportResultDto { Layer = layer };

        List<IFeature> features;
        try
        {
            features = ReadFeatures(geojsonStream);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse GeoJSON: {ex.Message}");
            return result;
        }

        result.TotalFeatures = features.Count;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var counters = new ImportCounters();

        foreach (var feature in features)
        {
            try
            {
                await featureHandler(feature, counters, ct);
            }
            catch (Exception ex)
            {
                counters.Skipped++;
                result.Errors.Add($"Feature skipped: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        result.Inserted = counters.Inserted;
        result.Updated = counters.Updated;
        result.Skipped = counters.Skipped;
        result.Success = result.Errors.Count == 0;
        return result;
    }

    // ─────────────────────────────────────────────────────────
    //  Per-level handlers
    // ─────────────────────────────────────────────────────────

    private async Task ImportAdm0FeatureAsync(IFeature feature, ImportCounters counters, CancellationToken ct)
    {
        var props = feature.Attributes;

        var pcode = GetString(props, "adm0_pcode", "ADM0_PCODE", "pcode", "PCODE")
            ?? throw new InvalidOperationException("Missing pcode (adm0_pcode)");

        var nameEn = GetString(props, "adm0_name", "ADM0_EN", "name_en", "NAME_EN", "name")
            ?? throw new InvalidOperationException("Missing name_en (adm0_name)");

        var nameAr = GetString(props, "adm0_name1", "ADM0_AR", "name_ar", "NAME_AR");

        var geom = ExtractMultiPolygon(feature.Geometry, pcode);

        var existing = await db.Adm0.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);
        if (existing is null)
        {
            db.Adm0.Add(new Adm0
            {
                NameEn = nameEn,
                NameAr = nameAr,
                Pcode = pcode,
                BoundaryGeom = geom,
            });
            counters.Inserted++;
        }
        else
        {
            existing.NameEn = nameEn;
            existing.NameAr = nameAr;
            existing.BoundaryGeom = geom;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            counters.Updated++;
        }
    }

    private async Task ImportAdm1FeatureAsync(IFeature feature, ImportCounters counters, CancellationToken ct)
    {
        var props = feature.Attributes;

        var pcode = GetString(props, "adm1_pcode", "ADM1_PCODE", "pcode", "PCODE")
            ?? throw new InvalidOperationException("Missing pcode (adm1_pcode)");

        var nameEn = GetString(props, "adm1_name", "ADM1_EN", "name_en", "NAME_EN", "name")
            ?? throw new InvalidOperationException("Missing name_en (adm1_name)");

        var nameAr = GetString(props, "adm1_name1", "ADM1_AR", "name_ar", "NAME_AR");

        var parentPcode = GetString(props, "adm0_pcode", "ADM0_PCODE")
            ?? throw new InvalidOperationException("Missing parent pcode (adm0_pcode)");

        var parent = await db.Adm0.FirstOrDefaultAsync(x => x.Pcode == parentPcode, ct)
            ?? throw new InvalidOperationException($"Parent adm0 not found for pcode '{parentPcode}'");

        var geom = ExtractMultiPolygon(feature.Geometry, pcode);

        var existing = await db.Adm1.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);
        if (existing is null)
        {
            db.Adm1.Add(new Adm1
            {
                Adm0Gid = parent.Gid,
                NameEn = nameEn,
                NameAr = nameAr,
                Pcode = pcode,
                BoundaryGeom = geom,
            });
            counters.Inserted++;
        }
        else
        {
            existing.NameEn = nameEn;
            existing.NameAr = nameAr;
            existing.BoundaryGeom = geom;
            existing.Adm0Gid = parent.Gid;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            counters.Updated++;
        }
    }

    private async Task ImportAdm2FeatureAsync(IFeature feature, ImportCounters counters, CancellationToken ct)
    {
        var props = feature.Attributes;

        var pcode = GetString(props, "adm2_pcode", "ADM2_PCODE", "pcode", "PCODE")
            ?? throw new InvalidOperationException("Missing pcode (adm2_pcode)");

        var nameEn = GetString(props, "adm2_name", "ADM2_EN", "name_en", "NAME_EN", "name")
            ?? throw new InvalidOperationException("Missing name_en (adm2_name)");

        var nameAr = GetString(props, "adm2_name1", "ADM2_AR", "name_ar", "NAME_AR");

        var parentPcode = GetString(props, "adm1_pcode", "ADM1_PCODE")
            ?? throw new InvalidOperationException("Missing parent pcode (adm1_pcode)");

        var parent = await db.Adm1.FirstOrDefaultAsync(x => x.Pcode == parentPcode, ct)
            ?? throw new InvalidOperationException($"Parent adm1 not found for pcode '{parentPcode}'");

        var geom = ExtractMultiPolygon(feature.Geometry, pcode);

        var existing = await db.Adm2.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);
        if (existing is null)
        {
            db.Adm2.Add(new Adm2
            {
                Adm1Gid = parent.Gid,
                NameEn = nameEn,
                NameAr = nameAr,
                Pcode = pcode,
                BoundaryGeom = geom,
            });
            counters.Inserted++;
        }
        else
        {
            existing.NameEn = nameEn;
            existing.NameAr = nameAr;
            existing.BoundaryGeom = geom;
            existing.Adm1Gid = parent.Gid;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            counters.Updated++;
        }
    }

    private async Task ImportAdm3FeatureAsync(IFeature feature, ImportCounters counters, CancellationToken ct)
    {
        var props = feature.Attributes;

        var pcode = GetString(props, "adm3_pcode", "ADM3_PCODE", "pcode", "PCODE")
            ?? throw new InvalidOperationException("Missing pcode (adm3_pcode)");

        var nameEn = GetString(props, "adm3_name", "ADM3_EN", "name_en", "NAME_EN", "name");

        var nameAr = GetString(props, "adm3_name1", "ADM3_AR", "name_ar", "NAME_AR");

        var parentPcode = GetString(props, "adm2_pcode", "ADM2_PCODE")
            ?? throw new InvalidOperationException("Missing parent pcode (adm2_pcode)");

        var parent = await db.Adm2.FirstOrDefaultAsync(x => x.Pcode == parentPcode, ct)
            ?? throw new InvalidOperationException($"Parent adm2 not found for pcode '{parentPcode}'");

        var geom = ExtractMultiPolygon(feature.Geometry, pcode);

        var existing = await db.Adm3.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);
        if (existing is null)
        {
            db.Adm3.Add(new Adm3
            {
                Adm2Gid = parent.Gid,
                NameEn = nameEn,
                NameAr = nameAr,
                Pcode = pcode,
                BoundaryGeom = geom,
            });
            counters.Inserted++;
        }
        else
        {
            existing.NameEn = nameEn;
            existing.NameAr = nameAr;
            existing.BoundaryGeom = geom;
            existing.Adm2Gid = parent.Gid;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            counters.Updated++;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static List<IFeature> ReadFeatures(Stream stream)
    {
        var serializer = GeoJsonSerializer.Create();
        using var sr = new StreamReader(stream, leaveOpen: true);
        using var jr = new Newtonsoft.Json.JsonTextReader(sr);

        var collection = serializer.Deserialize<FeatureCollection>(jr)
            ?? throw new InvalidOperationException("GeoJSON stream is empty or invalid.");

        return [.. collection];
    }

    /// <summary>Extracts a MultiPolygon from the feature geometry, converting Polygon if needed. Forces SRID 4326.</summary>
    private static MultiPolygon ExtractMultiPolygon(Geometry? geometry, string pcode)
    {
        if (geometry is null)
            throw new InvalidOperationException($"Feature '{pcode}' has null geometry.");

        MultiPolygon? multi = geometry switch
        {
            MultiPolygon mp => mp,
            Polygon p => new MultiPolygon([p]),
            _ => throw new InvalidOperationException(
                $"Feature '{pcode}' has unsupported geometry type '{geometry.GeometryType}'. Expected Polygon or MultiPolygon.")
        };

        multi.SRID = 4326;
        foreach (var geom in multi.Geometries)
            geom.SRID = 4326;

        return multi;
    }

    /// <summary>Returns the first non-null, non-whitespace value found among the given key aliases.</summary>
    private static string? GetString(IAttributesTable props, params string[] keys)
    {
        foreach (var key in keys)
        {
            try
            {
                if (props.Exists(key))
                {
                    var val = props[key]?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            catch { /* IAttributesTable may throw on missing keys in some impls */ }
        }
        return null;
    }

    private sealed class ImportCounters
    {
        public int Inserted;
        public int Updated;
        public int Skipped;
    }
}
