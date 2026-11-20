# Configura la contraseña de la base de datos de Supabase en la API C#.
# Ejecutar desde PowerShell:  .\configurar-db.ps1

$ErrorActionPreference = 'Stop'
$appsettings = Join-Path $PSScriptRoot 'src\TecnoMarket.Api\appsettings.json'

Write-Host 'Dónde obtenerla: Supabase > Project Settings > Database > Connection string (password).' -ForegroundColor Cyan
$segura = Read-Host 'Pega la contraseña de la BD' -AsSecureString
$claro = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura))

$cadena = "Host=db.uvllrtzcatnrfopjtohs.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=$claro;SSL Mode=Require;Trust Server Certificate=true;Timeout=15"

$json = Get-Content $appsettings -Raw | ConvertFrom-Json
$json.ConnectionStrings.Supabase = $cadena
$json | ConvertTo-Json -Depth 10 | Set-Content $appsettings -Encoding UTF8

Write-Host 'Listo. Ahora ejecuta: dotnet run --project src\TecnoMarket.Api' -ForegroundColor Green
