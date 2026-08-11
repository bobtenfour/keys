# KeyInventory — Docker Evaluation Deployment (shared Droplet)

**DEMO / EVALUATION ONLY.** Deploys KeyInventory beside DentalInventory on the same DigitalOcean Droplet without sharing databases or modifying `/opt/Inv`.

| Aspect | Value |
| --- | --- |
| Droplet path | `/opt/keys` |
| Compose project | `keyinventory-demo` |
| Web host URL | `http://159.203.182.9:8081` |
| SQL Server | Existing `dentalinventory-demo-sql` on network `dentalinventory-demo_default` (alias `sqlserver:1433`) |
| Database | **`KeyInventoryDemo` only** |
| Forbidden DBs | `DentalInventoryDemo`, `DentalInventoryDev`, `KeyInventoryDev` |

## Isolation rules

- Do **not** create a second SQL Server container.
- Do **not** modify `/opt/Inv`, DentalInventory compose, containers, volumes, or `DentalInventoryDemo`.
- KeyInventory containers attach only to external network `dentalinventory-demo_default`.
- Migrations and Web both use `ConnectionStrings__KeyInventory` → `Database=KeyInventoryDemo`.

## Containers

| Container | Role |
| --- | --- |
| `keyinventory-demo-migrate` | One-shot `dotnet ef database update`; exits 0 on success |
| `keyinventory-demo-web` | Kestrel on `0.0.0.0:${PORT}` (default 8080), published as host `8081` |

Web does **not** apply EF migrations at startup in Demo (`KeyInventory:ApplyMigrationsOnStartup=false`).

## Evaluation account

| Setting | Source |
| --- | --- |
| Username | `LocalBootstrapAdmin__UserName` (recommended `user`) |
| Password | `LocalBootstrapAdmin__Password` (runtime secret only) |
| Email | `LocalBootstrapAdmin__Email` (default `user@demo.local`) |

Bootstrap requires `ASPNETCORE_ENVIRONMENT=Demo` **and** `LocalBootstrapAdmin__Enabled=true`. Committed `docker/appsettings.Demo.json` keeps `Enabled=false`.

## Server deployment commands

Run on the Droplet after DentalInventory SQL is healthy and network `dentalinventory-demo_default` exists:

```bash
sudo mkdir -p /opt/keys
# Copy KeyInventory repository contents to /opt/keys (git clone or rsync from your workstation).
cd /opt/keys
cp .env.demo.template .env.demo
# Edit .env.demo: set MSSQL_SA_PASSWORD (same as DentalInventory) and LocalBootstrapAdmin__Password.
nano .env.demo
chmod 600 .env.demo

docker compose -f docker-compose.demo.yml --env-file .env.demo build
docker compose -f docker-compose.demo.yml --env-file .env.demo up -d

docker compose -f docker-compose.demo.yml --env-file .env.demo ps
docker compose -f docker-compose.demo.yml --env-file .env.demo logs migrate
curl -fsS http://127.0.0.1:8081/health/ready
```

Public check: `http://159.203.182.9:8081` → log in with the Demo bootstrap user.

## Useful operations

```bash
cd /opt/keys
docker compose -f docker-compose.demo.yml --env-file .env.demo logs -f web
docker compose -f docker-compose.demo.yml --env-file .env.demo up -d --build
docker compose -f docker-compose.demo.yml --env-file .env.demo down
```

`down` removes only KeyInventory compose containers/networks links; it does **not** stop DentalInventory SQL or remove SQL volumes.
