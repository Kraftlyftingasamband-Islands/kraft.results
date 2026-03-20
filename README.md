# Populate local database with production backup

1. Export database dump from [Plesk](https://plesk8600.is.cc:8443/smb/database/list) and download the zip file.
1. Start the Aspire AppHost if not already running:

    ```bash
    dotnet run --project src/KRAFT.Results.AppHost
    ```

1. Run the restore script:

    ```bash
    ./restore-db.sh /path/to/dump.zip
    ```

    The script will extract the zip, copy the backup into the SQL container, and restore it to the `kraft-db` database.
