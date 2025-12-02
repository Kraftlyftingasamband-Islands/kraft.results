# Populate local database with production backup

1. Export database dump from [Plesk](https://plesk8600.is.cc:8443/smb/database/list) and download it.
1. Extract zip file. It contains a single file with no extension. Add a `.bak` extension to it.
1. Copy the `.bak` file to the SQL container (change the first part of the command to use the filename of your file).

    ```bash
    docker cp backup.bak kraft-sql:/var/opt/mssql/data/backup.bak
    ```

1. Run the following SQL script to get the logical data and log names

    ```sql
    RESTORE FILELISTONLY
    FROM DISK = '/var/opt/mssql/data/backup.bak';
    ```

1. Use `master` database

    ```sql
    USE master;
    ```

1. Set `kraft-db` database to only accept a single connection.

    ```sql
    ALTER DATABASE [kraft-db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ```

1. Restore the backup (the names after tha `MOVE` command are the logical data and log names).

    ```sql
    RESTORE DATABASE [kraft-db]
    FROM DISK = '/var/opt/mssql/data/backup.bak'
    WITH
        MOVE 'resultskraftisdev' TO '/var/opt/mssql/data/kraft-db.mdf',
        MOVE 'resultskraftisdev_log' TO '/var/opt/mssql/data/kraft-db.ldf',
        REPLACE,
        STATS = 10;
    ```

1. Set `kraft-db` to accept multiple connections.

    ```sql
    ALTER DATABASE [kraft-db] SET MULTI_USER;
    ```
