param(
    [string]$OutputFile = "Src/CurrencyCalculator.Web/wwwroot/fallback/latest-rates.json"
)

$ErrorActionPreference = "Stop"

$supportedCodes = @(
    "USD","CNY","EUR","JPY","GBP","HKD","MOP","SGD","AUD","CAD","CHF","NZD","TWD","BTC","ETH"
)

Write-Host "Fetching rates from fawaz exchange-api..."
$response = Invoke-RestMethod -Uri "https://latest.currency-api.pages.dev/v1/currencies/usd.json" -Method Get

if (-not $response.usd) {
    throw "Response does not contain usd object."
}

$rates = @{}
$rates["USD"] = 1

foreach ($code in $supportedCodes) {
    if ($code -eq "USD") {
        continue
    }

    $key = $code.ToLowerInvariant()
    $value = $response.usd.$key
    if ($null -ne $value) {
        $rates[$code] = [decimal]$value
    }
}

$payload = [ordered]@{
    baseCurrency = "USD"
    fetchedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    source = "fawaz.exchange-api"
    sourceKind = 2
    ratesFromBase = $rates
}

$directory = Split-Path -Path $OutputFile -Parent
if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputFile -Encoding UTF8
Write-Host "Fallback rates updated at $OutputFile"
