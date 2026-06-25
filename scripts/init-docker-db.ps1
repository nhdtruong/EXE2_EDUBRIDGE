param(
    [switch]$ResetVolume
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$container = 'edubridge-sqlserver'
$password = 'EduBridge_Test_2026!'

Push-Location $repo
try {
    if ($ResetVolume) {
        docker compose --progress quiet down -v
        if ($LASTEXITCODE -ne 0) { throw 'docker compose down failed' }
    }

    docker compose --progress quiet up -d sqlserver
    if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }

    Write-Host 'Waiting for SQL Server...'
    $ready = $false
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = 'SilentlyContinue'
        docker exec $container /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $password -C -I -Q 'SELECT 1' *> $null
        $ErrorActionPreference = $previousPreference
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw 'SQL Server did not become ready' }

    $scripts = @(
        'edubridge_database/init/00_create_database.sql'
    )
    $scripts += Get-ChildItem 'edubridge_database/migration/*.sql' |
        Where-Object Name -ne 'AddDataProtectionKeys.sql' |
        Sort-Object Name |
        ForEach-Object { $_.FullName.Substring($repo.Length + 1).Replace('\', '/') }
    $scripts += 'edubridge_database/seed/parent_app_mock.sql'

    foreach ($script in $scripts) {
        Write-Host "Running $script"
        $containerPath = "/tmp/" + [IO.Path]::GetFileName($script)
        docker cp (Join-Path $repo $script) "${container}:$containerPath"
        if ($LASTEXITCODE -ne 0) { throw "docker cp failed for $script" }
        docker exec $container /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $password -C -I -b -f 65001 -i $containerPath
        if ($LASTEXITCODE -ne 0) { throw "SQL script failed: $script" }
    }

    Write-Host 'Database initialized. Test login: parent@edubridge.com / 123456'
}
finally {
    Pop-Location
}
