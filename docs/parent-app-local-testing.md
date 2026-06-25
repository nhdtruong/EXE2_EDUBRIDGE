# Parent App Local Testing

## Start from a clean database

From `EXE2_EDUBRIDGE`:

```powershell
.\scripts\init-docker-db.ps1 -ResetVolume
dotnet run --launch-profile http
```

The database runner:

1. Starts SQL Server 2022 on `localhost:14333`.
2. Runs `edubridge_database/init/00_create_database.sql`.
3. Runs migrations in filename order.
4. Runs `edubridge_database/seed/parent_app_mock.sql`.

The init script intentionally recreates `EduBridgeDB`; use it only for local/test data.

## Test accounts

All seeded accounts use password `123456`.

| Role | Login |
| --- | --- |
| Parent | `parent@edubridge.com` |
| Teacher | `teacher@edubridge.com` |
| Owner | `owner@edubridge.com` |

The Parent account has two children and mock data for today's schedule, attendance alert, lesson diary, homework, grade, unpaid invoice, notification, chat, and leave request.

## Mobile URL

The default app configuration points Android Emulator to `http://10.0.2.2:5253`.

- Android Emulator: use `10.0.2.2`.
- iOS Simulator/Web: override `expo.extra.apiBaseUrl` to `http://localhost:5253`.
- Physical device: use the development machine's LAN IP and allow port `5253`.

## Useful commands

```powershell
docker compose ps
docker compose logs sqlserver
docker compose stop
docker compose down
```
