using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Services;

public class NewsHistoryStore(
    IWebHostEnvironment environment,
    IOptions<SafetyIndexOptions> options,
    ILogger<NewsHistoryStore> logger) : INewsHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string baseDirectory = Path.IsPathRooted(options.Value.NewsHistoryDirectory)
        ? options.Value.NewsHistoryDirectory
        : Path.Combine(environment.ContentRootPath, options.Value.NewsHistoryDirectory);

    public async Task<List<NewsItemDto>> MergeAsync(
        int adm2Gid,
        IEnumerable<NewsItemDto> newsItems,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(baseDirectory);

        var existing = await ReadAsync(adm2Gid, ct);
        var byKey = existing
            .GroupBy(GetStableKey)
            .ToDictionary(x => x.Key, x => x.First());

        var added = 0;
        foreach (var item in newsItems)
        {
            var key = GetStableKey(item);
            if (byKey.ContainsKey(key))
            {
                continue;
            }

            byKey[key] = item;
            added++;
        }

        var merged = byKey.Values
            .OrderByDescending(x => x.PublishedAtUtc ?? x.FetchedAtUtc)
            .ToList();

        var path = GetPath(adm2Gid);
        var tempPath = $"{path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, merged, JsonOptions, ct);
        }

        File.Move(tempPath, path, overwrite: true);

        logger.LogInformation(
            "Saved safety news history for adm2 {Adm2Gid}. Added {AddedCount}, total {TotalCount}",
            adm2Gid,
            added,
            merged.Count);

        return merged;
    }

    public async Task<List<NewsItemDto>> ReadAsync(int adm2Gid, CancellationToken ct = default)
    {
        var path = GetPath(adm2Gid);
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<NewsItemDto>>(stream, JsonOptions, ct) ?? [];
    }

    private string GetPath(int adm2Gid) => Path.Combine(baseDirectory, $"adm2-{adm2Gid}.json");

    private static string GetStableKey(NewsItemDto item)
    {
        var key = !string.IsNullOrWhiteSpace(item.Guid)
            ? item.Guid
            : !string.IsNullOrWhiteSpace(item.Link)
                ? item.Link
                : $"{item.Title}|{item.PublishedAtUtc:O}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }
}
