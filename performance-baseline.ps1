Add-Type -AssemblyName System.Net.Http

$BaseUrl = "http://localhost:5160"
$RequestCount = 200
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

$Results = @()

foreach ($Endpoint in $Endpoints) {
    Write-Host ""
    Write-Host "Testing $($Endpoint.Name)..."

    for ($i = 1; $i -le $WarmupCount; $i++) {
        try {
            $WarmupResponse = $Client.GetAsync($Endpoint.Url).Result
            $WarmupResponse.Dispose()
        }
        catch {
        }
    }

    $Durations = New-Object 'System.Collections.Generic.List[double]'
    $Successful = 0
    $Failed = 0

    $TotalWatch = [Diagnostics.Stopwatch]::StartNew()

    for ($i = 1; $i -le $RequestCount; $i++) {
        $RequestWatch = [Diagnostics.Stopwatch]::StartNew()

        try {
            $Response = $Client.GetAsync($Endpoint.Url).Result
            $RequestWatch.Stop()

            $Durations.Add($RequestWatch.Elapsed.TotalMilliseconds)

            $StatusCode = [int]$Response.StatusCode

            if ($StatusCode -ge 200 -and $StatusCode -lt 400) {
                $Successful++
            }
            else {
                $Failed++
            }

            $Response.Dispose()
        }
        catch {
            $RequestWatch.Stop()
            $Failed++
        }
    }

    $TotalWatch.Stop()

    if ($Durations.Count -gt 0) {
        $Statistics = $Durations | Measure-Object -Minimum -Maximum -Average

        $AverageMs = [Math]::Round($Statistics.Average, 2)
        $MinimumMs = [Math]::Round($Statistics.Minimum, 2)
        $MaximumMs = [Math]::Round($Statistics.Maximum, 2)
        $P50Ms = Get-Percentile -Values $Durations.ToArray() -Percent 50
        $P95Ms = Get-Percentile -Values $Durations.ToArray() -Percent 95
        $P99Ms = Get-Percentile -Values $Durations.ToArray() -Percent 99
    }
    else {
        $AverageMs = 0
        $MinimumMs = 0
        $MaximumMs = 0
        $P50Ms = 0
        $P95Ms = 0
        $P99Ms = 0
    }

    $Results += [pscustomobject]@{
        Endpoint = $Endpoint.Name
        Requests = $RequestCount
        Successful = $Successful
        Failed = $Failed
        AverageMs = $AverageMs
        MinimumMs = $MinimumMs
        P50Ms = $P50Ms
        P95Ms = $P95Ms
        P99Ms = $P99Ms
        MaximumMs = $MaximumMs
        RequestsPerSecond = [Math]::Round(
            $RequestCount / $TotalWatch.Elapsed.TotalSeconds,
            2
        )
    }
}

$Client.Dispose()

New-Item -ItemType Directory -Force -Path ".\TestResults" | Out-Null

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$CsvPath = ".\TestResults\Performance-Baseline-$Timestamp.csv"
$JsonPath = ".\TestResults\Performance-Baseline-$Timestamp.json"

$Results | Export-Csv -Path $CsvPath -NoTypeInformation
$Results | ConvertTo-Json | Set-Content -Path $JsonPath -Encoding UTF8

Write-Host ""
$Results | Format-Table -AutoSize

Write-Host ""
Write-Host "CSV report:  $CsvPath"
Write-Host "JSON report: $JsonPath"
