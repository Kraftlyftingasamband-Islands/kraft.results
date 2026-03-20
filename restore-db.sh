#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME="kraft-sql"
DB_NAME="kraft-db"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
BACKUP_DEST="/var/opt/mssql/data/backup.bak"
APPHOST_PROJECT="src/KRAFT.Results.AppHost"
LOGICAL_DATA="resultskraftisdev"
LOGICAL_LOG="resultskraftisdev_log"

if [[ $# -eq 0 ]]; then
    DOWNLOADS="$USERPROFILE/Downloads"
    BACKUP_FILE=$(find "$DOWNLOADS" -maxdepth 1 -name 'resultskraftisdev_*.zip' -printf '%T@ %p\n' 2>/dev/null \
        | sort -rn | head -n 1 | cut -d' ' -f2-)
    if [[ -z "$BACKUP_FILE" ]]; then
        echo "Error: No resultskraftisdev_*.zip found in $DOWNLOADS" >&2
        exit 1
    fi
    echo "Found backup: $BACKUP_FILE"
elif [[ $# -eq 1 ]]; then
    BACKUP_FILE="$1"
else
    echo "Usage: $0 [/path/to/dump.zip]" >&2
    exit 1
fi

if [[ ! -f "$BACKUP_FILE" ]]; then
    echo "Error: File not found: $BACKUP_FILE" >&2
    exit 1
fi

if [[ "$BACKUP_FILE" == *.zip ]]; then
    TEMP_DIR=$(mktemp -d)
    trap 'rm -rf "$TEMP_DIR"' EXIT
    unzip -j -q "$BACKUP_FILE" -d "$TEMP_DIR"
    BACKUP_FILE=$(find "$TEMP_DIR" -type f | head -n 1)
    if [[ -z "$BACKUP_FILE" ]]; then
        echo "Error: No file found inside the zip archive." >&2
        exit 1
    fi
fi

if [[ "$(docker inspect --format='{{.State.Running}}' "$CONTAINER_NAME" 2>/dev/null)" != "true" ]]; then
    echo "Error: Container '$CONTAINER_NAME' is not running. Start it with: dotnet run --project $APPHOST_PROJECT" >&2
    exit 1
fi

SA_PASSWORD=$(dotnet user-secrets list --project "$APPHOST_PROJECT" 2>/dev/null \
    | grep 'Parameters:sql-password' \
    | sed 's/^.*= //')

if [[ -z "$SA_PASSWORD" ]]; then
    echo "Error: Could not find SA password in user-secrets for $APPHOST_PROJECT." >&2
    exit 1
fi

echo "Copying backup into container..."
docker cp "$BACKUP_FILE" "$CONTAINER_NAME:$BACKUP_DEST"

echo "Restoring database..."
MSYS_NO_PATHCONV=1 docker exec -e SQLCMDPASSWORD="$SA_PASSWORD" "$CONTAINER_NAME" "$SQLCMD" \
    -S localhost -U sa -C -Q "
        ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        RESTORE DATABASE [$DB_NAME]
        FROM DISK = '$BACKUP_DEST'
        WITH
            MOVE '$LOGICAL_DATA' TO '/var/opt/mssql/data/kraft-db.mdf',
            MOVE '$LOGICAL_LOG' TO '/var/opt/mssql/data/kraft-db.ldf',
            REPLACE,
            STATS = 10;
        ALTER DATABASE [$DB_NAME] SET MULTI_USER;
    "

echo "Seeding migration history..."
MIGRATIONS=$(ls src/KRAFT.Results.WebApi/Migrations/*.cs \
    | grep -v Designer | grep -v Snapshot \
    | sed 's|.*/||; s|\.cs$||')

SEED_SQL="IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;"

while IFS= read -r migration; do
    SEED_SQL="$SEED_SQL
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '$migration')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('$migration', '10.0.0');"
done <<< "$MIGRATIONS"

MSYS_NO_PATHCONV=1 docker exec -e SQLCMDPASSWORD="$SA_PASSWORD" "$CONTAINER_NAME" "$SQLCMD" \
    -S localhost -U sa -C -d "$DB_NAME" -Q "$SEED_SQL"

echo "Applying migrations..."
DB_PORT=$(docker port "$CONTAINER_NAME" 1433 | head -n 1 | cut -d: -f2)
CONNECTION_STRING="Server=127.0.0.1,$DB_PORT;Database=$DB_NAME;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True"
dotnet ef database update --project src/KRAFT.Results.WebApi --connection "$CONNECTION_STRING"

echo "Database '$DB_NAME' restored and migrations applied successfully."
