# Inicia la API, la web y dos tuneles publicos de Cloudflare.
# Ejecutar: powershell -ExecutionPolicy Bypass -File .\iniciar_nube.ps1

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$logs = Join-Path $raiz 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null

function Iniciar($nombre, $archivo, $argumentos) {
    $salida = Join-Path $logs ($nombre + '.log')
    $errores = Join-Path $logs ($nombre + '.err.log')
    Start-Process -FilePath $archivo -ArgumentList $argumentos `
        -RedirectStandardOutput $salida -RedirectStandardError $errores `
        -WindowStyle Hidden -PassThru
}

$api = Iniciar 'api' 'dotnet' @('run','--project',(Join-Path $raiz 'src\TecnoMarket.Api\TecnoMarket.Api.csproj'),'--no-launch-profile','--urls','http://localhost:5204','--no-build')
Start-Sleep -Seconds 4

$web = Iniciar 'web' 'dotnet' @('run','--project',(Join-Path $raiz 'src\TecnoMarket.Web\TecnoMarket.Web.csproj'),'--no-launch-profile','--urls','http://localhost:5000','--no-build')
Start-Sleep -Seconds 4

$cloud = Join-Path $raiz 'tools\cloudflared.exe'
$tunelApi = Iniciar 'tunel-api' $cloud @('tunnel','--url','http://localhost:5204','--no-autoupdate','--metrics','127.0.0.1:20241')
$tunelWeb = Iniciar 'tunel-web' $cloud @('tunnel','--url','http://localhost:5000','--no-autoupdate','--metrics','127.0.0.1:20242')
Start-Sleep -Seconds 12

function BuscarUrl($nombre) {
    $salida = Join-Path $logs ($nombre + '.log')
    $errores = Join-Path $logs ($nombre + '.err.log')
    $linea = Select-String -Path @($salida, $errores) -Pattern 'https://[a-z0-9-]+\.trycloudflare\.com' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($linea) { return $linea.Matches[0].Value }
    return '(no disponible aun)'
}

$urlApi = BuscarUrl 'tunel-api'
$urlWeb = BuscarUrl 'tunel-web'

@(
    'TECNOMARKET EN LA NUBE',
    "API : $urlApi",
    "WEB : $urlWeb",
    '',
    'Nota: las URLs de Cloudflare quick tunnel cambian en cada reinicio.',
    'Si cambian, actualiza ApiConfig.kt y src\TecnoMarket.Web\appsettings.json.',
    '',
    "PIDs: api=$($api.Id) web=$($web.Id) tunelApi=$($tunelApi.Id) tunelWeb=$($tunelWeb.Id)"
) | Tee-Object (Join-Path $raiz 'URLS.txt')
