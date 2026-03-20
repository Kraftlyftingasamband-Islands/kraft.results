param(
    [string]$Path
)

$ErrorActionPreference = 'Stop'

$ContainerName = 'kraft-sql'
$DbName = 'kraft-db'
$Sqlcmd = '/opt/mssql-tools18/bin/sqlcmd'
$BackupDest = '/var/opt/mssql/data/backup.bak'
$AppHostProject = 'src/KRAFT.Results.AppHost'
$LogicalData = 'resultskraftisdev'
$LogicalLog = 'resultskraftisdev_log'

if (-not $Path) {
    $downloads = Join-Path $env:USERPROFILE 'Downloads'
    $file = Get-ChildItem -Path $downloads -Filter 'resultskraftisdev_*.zip' -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $file) {
        Write-Error "No resultskraftisdev_*.zip found in $downloads"
        exit 1
    }

    $backupFile = $file.FullName
    Write-Host "Found backup: $backupFile"
}
else {
    $backupFile = $Path
}

if (-not (Test-Path $backupFile)) {
    Write-Error "File not found: $backupFile"
    exit 1
}

$tempDir = $null

try {
    if ($backupFile -like '*.zip') {
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
        Expand-Archive -Path $backupFile -DestinationPath $tempDir -Force

        $extracted = Get-ChildItem -Path $tempDir -File -Recurse -Force | Select-Object -First 1
        if (-not $extracted) {
            Write-Error 'No file found inside the zip archive.'
            exit 1
        }

        $backupFile = $extracted.FullName
    }

    $running = docker inspect --format='{{.State.Running}}' $ContainerName 2>$null
    if ($running -ne 'true') {
        Write-Error "Container '$ContainerName' is not running. Start it with: dotnet run --project $AppHostProject"
        exit 1
    }

    $saPassword = (dotnet user-secrets list --project $AppHostProject 2>$null |
        Select-String 'Parameters:sql-password' |
        ForEach-Object { ($_ -split ' = ', 2)[1] })
    if (-not $saPassword) {
        Write-Error "Could not find SA password in user-secrets for $AppHostProject."
        exit 1
    }

    Write-Host 'Copying backup into container...'
    docker cp $backupFile "${ContainerName}:${BackupDest}"

    Write-Host 'Restoring database...'
    docker exec -e "SQLCMDPASSWORD=$saPassword" $ContainerName $Sqlcmd `
        -S localhost -U sa -C -Q @"
        ALTER DATABASE [$DbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        RESTORE DATABASE [$DbName]
        FROM DISK = '$BackupDest'
        WITH
            MOVE '$LogicalData' TO '/var/opt/mssql/data/kraft-db.mdf',
            MOVE '$LogicalLog' TO '/var/opt/mssql/data/kraft-db.ldf',
            REPLACE,
            STATS = 10;
        ALTER DATABASE [$DbName] SET MULTI_USER;
"@

    Write-Host 'Seeding migration history...'
    $migrations = Get-ChildItem -Path src/KRAFT.Results.WebApi/Migrations/*.cs |
        Where-Object { $_.Name -notmatch 'Designer|Snapshot' } |
        ForEach-Object { $_.BaseName }

    $seedSql = @"
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
"@

    foreach ($migration in $migrations) {
        $seedSql += "`nIF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '$migration')"
        $seedSql += "`n    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('$migration', '10.0.0');"
    }

    docker exec -e "SQLCMDPASSWORD=$saPassword" $ContainerName $Sqlcmd `
        -S localhost -U sa -C -d $DbName -Q $seedSql

    Write-Host 'Applying migrations...'
    $dbPort = (docker port $ContainerName 1433 | Select-Object -First 1) -replace '.*:', ''
    $connectionString = "Server=127.0.0.1,$dbPort;Database=$DbName;User Id=sa;Password=$saPassword;TrustServerCertificate=True"
    dotnet ef database update --project src/KRAFT.Results.WebApi --connection $connectionString

    Write-Host "Database '$DbName' restored and migrations applied successfully."
}
finally {
    if ($tempDir -and (Test-Path $tempDir)) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
}
