param(
    [string]$OutputDir = "Src/CurrencyCalculator.Web/wwwroot/img/fiat_icons"
)

$ErrorActionPreference = "Stop"

# Fiat icon codes from CurrencyCatalog.cs (2-letter country/region codes)
$iconCodes = @(
    "us", "cn", "eu", "jp", "gb", "hk", "mo", "sg", "au", "ca", "ch", "nz", "tw"
)

$baseUrl = "https://raw.githubusercontent.com/lipis/flag-icons/main/flags/4x3"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "Created directory: $OutputDir"
}

foreach ($code in $iconCodes) {
    $url = "$baseUrl/$code.svg"
    $outFile = Join-Path $OutputDir "$code.svg"
    Write-Host "Downloading $code.svg from $url ..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $outFile -UseBasicParsing
        Write-Host "  OK: $outFile"
    }
    catch {
        Write-Warning "  FAILED to download $code.svg: $_"
    }
}

Write-Host "Fiat icons updated in $OutputDir"
