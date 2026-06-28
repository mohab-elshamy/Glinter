using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Glinter.Shared.Infrastructure.Metrics;

public sealed class ApplicationMetrics
{
    private static readonly double[] DurationBucketsMilliseconds =
        [5, 25, 100, 250, 500, 1000, 2500, 5000];

    private readonly ConcurrentDictionary<(string Method, string StatusClass), long>
        _requestCounts = new();
    private readonly long[] _durationBuckets =
        new long[DurationBucketsMilliseconds.Length + 1];
    private readonly object _durationLock = new();
    private long _requestsInFlight;
    private long _durationCount;
    private double _durationSumMilliseconds;
    private long _cleanupRuns;
    private long _cleanupFailures;
    private long _cleanupRowsDeleted;

    public void RequestStarted() => Interlocked.Increment(ref _requestsInFlight);

    public void RequestCompleted(string method, int statusCode, double elapsedMilliseconds)
    {
        Interlocked.Decrement(ref _requestsInFlight);
        _requestCounts.AddOrUpdate(
            (NormalizeMethod(method), $"{Math.Clamp(statusCode / 100, 1, 5)}xx"),
            1,
            (_, count) => count + 1);

        lock (_durationLock)
        {
            _durationCount++;
            _durationSumMilliseconds += elapsedMilliseconds;
            var bucketIndex = Array.FindIndex(
                DurationBucketsMilliseconds,
                boundary => elapsedMilliseconds <= boundary);
            if (bucketIndex < 0)
                bucketIndex = _durationBuckets.Length - 1;
            _durationBuckets[bucketIndex]++;
        }
    }

    public void CleanupCompleted(int rowsDeleted)
    {
        Interlocked.Increment(ref _cleanupRuns);
        Interlocked.Add(ref _cleanupRowsDeleted, rowsDeleted);
    }

    public void CleanupFailed()
    {
        Interlocked.Increment(ref _cleanupRuns);
        Interlocked.Increment(ref _cleanupFailures);
    }

    public string RenderPrometheus()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# HELP glinter_http_requests_total Completed HTTP requests.");
        builder.AppendLine("# TYPE glinter_http_requests_total counter");
        foreach (var item in _requestCounts.OrderBy(x => x.Key.Method)
                     .ThenBy(x => x.Key.StatusClass))
        {
            builder.Append("glinter_http_requests_total{method=\"")
                .Append(item.Key.Method)
                .Append("\",status_class=\"")
                .Append(item.Key.StatusClass)
                .Append("\"} ")
                .AppendLine(item.Value.ToString(CultureInfo.InvariantCulture));
        }

        builder.AppendLine("# HELP glinter_http_requests_in_flight Current HTTP requests.");
        builder.AppendLine("# TYPE glinter_http_requests_in_flight gauge");
        builder.Append("glinter_http_requests_in_flight ")
            .AppendLine(Interlocked.Read(ref _requestsInFlight)
                .ToString(CultureInfo.InvariantCulture));

        long[] buckets;
        long durationCount;
        double durationSum;
        lock (_durationLock)
        {
            buckets = [.. _durationBuckets];
            durationCount = _durationCount;
            durationSum = _durationSumMilliseconds;
        }

        builder.AppendLine(
            "# HELP glinter_http_request_duration_milliseconds HTTP request duration.");
        builder.AppendLine(
            "# TYPE glinter_http_request_duration_milliseconds histogram");
        long cumulative = 0;
        for (var index = 0; index < buckets.Length; index++)
        {
            cumulative += buckets[index];
            var boundary = index < DurationBucketsMilliseconds.Length
                ? DurationBucketsMilliseconds[index].ToString(CultureInfo.InvariantCulture)
                : "+Inf";
            builder.Append(
                    "glinter_http_request_duration_milliseconds_bucket{le=\"")
                .Append(boundary)
                .Append("\"} ")
                .AppendLine(cumulative.ToString(CultureInfo.InvariantCulture));
        }
        builder.Append("glinter_http_request_duration_milliseconds_sum ")
            .AppendLine(durationSum.ToString(CultureInfo.InvariantCulture));
        builder.Append("glinter_http_request_duration_milliseconds_count ")
            .AppendLine(durationCount.ToString(CultureInfo.InvariantCulture));

        builder.AppendLine("# HELP glinter_cleanup_runs_total Cleanup worker runs.");
        builder.AppendLine("# TYPE glinter_cleanup_runs_total counter");
        builder.Append("glinter_cleanup_runs_total ")
            .AppendLine(Interlocked.Read(ref _cleanupRuns)
                .ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("# HELP glinter_cleanup_failures_total Cleanup worker failures.");
        builder.AppendLine("# TYPE glinter_cleanup_failures_total counter");
        builder.Append("glinter_cleanup_failures_total ")
            .AppendLine(Interlocked.Read(ref _cleanupFailures)
                .ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("# HELP glinter_cleanup_rows_deleted_total Rows deleted by cleanup.");
        builder.AppendLine("# TYPE glinter_cleanup_rows_deleted_total counter");
        builder.Append("glinter_cleanup_rows_deleted_total ")
            .AppendLine(Interlocked.Read(ref _cleanupRowsDeleted)
                .ToString(CultureInfo.InvariantCulture));

        return builder.ToString();
    }

    private static string NormalizeMethod(string method) =>
        method.ToUpperInvariant() switch
        {
            "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS"
                => method.ToUpperInvariant(),
            _ => "OTHER"
        };
}
