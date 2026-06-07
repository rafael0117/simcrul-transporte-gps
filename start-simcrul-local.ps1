$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProject = Join-Path $root "SIMCRUL.API"
$webProject = Join-Path $root "SIMCRUL.Web"

Write-Host "Iniciando SIMCRUL.API en http://localhost:5272 ..."
Start-Process dotnet -ArgumentList "run --no-build" -WorkingDirectory $apiProject -Environment @{ ASPNETCORE_ENVIRONMENT = "Development" }

Start-Sleep -Seconds 5

Write-Host "Iniciando SIMCRUL.Web en http://localhost:5171 ..."
Start-Process dotnet -ArgumentList "run --no-build" -WorkingDirectory $webProject -Environment @{ ASPNETCORE_ENVIRONMENT = "Development" }

Write-Host ""
Write-Host "Abre estas rutas en tu navegador:"
Write-Host "  API Swagger: http://localhost:5272/swagger"
Write-Host "  Google Maps demo: http://localhost:5171/google-routes.html"
Write-Host "  Web MVC actual: http://localhost:5171/"
