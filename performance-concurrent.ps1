Add-Type -AssemblyName System.Net.Http

[System.Net.ServicePointManager]::DefaultConnectionLimit = 100

if (-not ("GlinterConcurrentRunner" -as [type])) {
    Add-Type -ReferencedAssemblies "System.Net.Http.dll" -TypeDefinition @"
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

public sealed class GlinterRequestResult
{
    public int StatusCode { get; set; }
    public double DurationMs { get; set; }
    public string Error { get; set; }
}

public static class GlinterConcurrentRunner
{
    private static readonly HttpClient Client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static async Task<List<GlinterRequestResult>> RunAsync(
        string url,
        int virtualUsers,
        int requestsPerUser)
    {
        var results = new ConcurrentBag<GlinterRequestResult>();
        var workers = new List<Task>();

        for (var user = 0; user < virtualUsers; user++)
        {
            workers.Add(Task.Run(async () =>
            {
                for (var request = 0; request < requestsPerUser; request++)
                {
                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        using (var response = await Client.GetAsync(url))
                        {
                            await response.Content.ReadAsByteArrayAsync();
                            stopwatch.Stop();

                            results.Add(new GlinterRequestResult
                            {
                                StatusCode = (int)response.StatusCode,
                                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                                Error = null
                            });
                        }
                    }
                    catch (Exception exception)
                    {
                        stopwatch.Stop();

                        results.Add(new GlinterRequestResult
                        {
                            StatusCode = 0,
                            DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                            Error = exception.GetType().Name
                        });
                    }
                }
            }));
        }

        await Task.WhenAll(workers);

        return new List<GlinterRequestResult>(results);
    }
}
"@
}

$BaseUrl = "http://localhost:5160"
$VirtualUsers = 10
$RequestsPerUser = 50
$WarmupCount = 20

$Endpoints = @(
    [pscustomobject]@{
        Name = "Health Live"
        Url = "$BaseUrl/health/live"
    },
    [pscustomobject]@{
        Name = "Stays List"
        Url = "$BaseUrl/api/stays?page=1&pageSize=20"
    },
    [pscustomobject]@{
        Name = "Experiences List"
        Url = "$BaseUrl/api/experiences?page=1&pageSize=20"
    }
)

function Get-Percentile {
    param(
        [double[]]$Values,
        [double]$Percent
    )

    if ($Values.Count -eq 0) {
        return 0
    }

    $Sorted = @($Values | Sort-Object)
    $Index = [Math]::Ceiling(($Percent / 100) * $Sorted.Count) - 1

    if ($Index -lt 0) {
        $Index = 0
    }

    return [Math]::Round([double]$Sorted[$Index], 2)
}

$Client = New-Object System.Net.Http.HttpClient
$Client.Timeout = [TimeSpan]::FromSeconds(30)

$Summary = @()
$Details = @()

foreach ($Endpoint in $Endpoints) {
    Write-Host ""
    Write-Host "Concurrent testing $($Endpoint.Name)..."

    for ($i = 1; $i -le $WarmupCount; $i++) {
        try {
            $WarmupResponse = $Client.GetAsync($Endpoint.Url).Result
            $WarmupResponse.Dispose()
        }
        catch {
        }
    }

    $TotalWatch = [Diagnostics.Stopwatch]::StartNew()

    $RawResults = [GlinterConcurrentRunner]::RunAsync(
        $Endpoint.Url,
        $VirtualUsers,
        $RequestsPerUser
    ).GetAwaiter().GetResult()

    $TotalWatch.Stop()

    $SuccessfulResults = @(
        $RawResults |
        Where-Object {
            $_.StatusCode -ge 200 -and
            $_.StatusCode -lt 400
        }
    )

    $FailedResults = @(
        $RawResults |
        Where-Object {
            $_.StatusCode -lt 200 -or
            $_.StatusCode -ge 400
        }
    )

    $Durations = @(
        $RawResults |
        Select-Object -ExpandProperty DurationMs
    )

    $Statistics = $Durations |
        Measure-Object -Minimum -Maximum -Average

    $RequestTotal = $VirtualUsers * $RequestsPerUser

    $Summary += [pscustomobject]@{
        Endpoint = $Endpoint.Name
        VirtualUsers = $VirtualUsers
        Requests = $RequestTotal
        Successful = $SuccessfulResults.Count
        Failed = $FailedResults.Count
        AverageMs = [Math]::Round($Statistics.Average, 2)
        MinimumMs = [Math]::Round($Statistics.Minimum, 2)
        P50Ms = Get-Percentile -Values $Durations -Percent 50
        P95Ms = Get-Percentile -Values $Durations -Percent 95
        P99Ms = Get-Percentile -Values $Durations -Percent 99
        MaximumMs = [Math]::Round($Statistics.Maximum, 2)
        RequestsPerSecond = [Math]::Round(
            $RequestTotal / $TotalWatch.Elapsed.TotalSeconds,
            2
        )
    }

    foreach ($Result in $RawResults) {
        $Details += [pscustomobject]@{
            Endpoint = $Endpoint.Name
            StatusCode = $Result.StatusCode
            DurationMs = [Math]::Round($Result.DurationMs, 2)
            Error = $Result.Error
        }
    }
}

$Client.Dispose()

New-Item `
    -ItemType Directory `
    -Force `
    -Path ".\TestResults" |
    Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

$SummaryCsv =
    ".\TestResults\Performance-Concurrent-Summary-$Timestamp.csv"

$SummaryJson =
    ".\TestResults\Performance-Concurrent-Summary-$Timestamp.json"

$DetailsCsv =
    ".\TestResults\Performance-Concurrent-Details-$Timestamp.csv"

$Summary |
    Export-Csv `
        -Path $SummaryCsv `
        -NoTypeInformation

$Summary |
    ConvertTo-Json |
    Set-Content `
        -Path $SummaryJson `
        -Encoding UTF8

$Details |
    Export-Csv `
        -Path $DetailsCsv `
        -NoTypeInformation

Write-Host ""
$Summary | Format-Table -AutoSize

Write-Host ""
Write-Host "Summary CSV:  $SummaryCsv"
Write-Host "Summary JSON: $SummaryJson"
Write-Host "Details CSV:  $DetailsCsv"
