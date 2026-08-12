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
- Do **not** use `docker compose down -v`, `docker volume rm`, or SQL Server container/volume deletion for KeyInventory operations.

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

There is **no** business-data seed. After a clean migrate, Departments/Rooms/KEY #/MEDECO/Loans start empty.

---

## NORMAL DEPLOY

**Preserves** existing `KeyInventoryDemo` data. Non-destructive. Does **not** drop the database.

Use after DentalInventory SQL is healthy and network `dentalinventory-demo_default` exists.

```bash
cd /opt/keys
# Ensure source matches origin/master (after git pull / reset from your workstation push).
test -f .env.demo
# First-time only: cp .env.demo.template .env.demo && edit MSSQL_SA_PASSWORD + LocalBootstrapAdmin__Password && chmod 600 .env.demo

docker compose -f docker-compose.demo.yml --env-file .env.demo build
docker compose -f docker-compose.demo.yml --env-file .env.demo up -d

docker compose -f docker-compose.demo.yml --env-file .env.demo ps
docker compose -f docker-compose.demo.yml --env-file .env.demo logs migrate
curl -fsS http://127.0.0.1:8081/health/ready
```

Public check: `http://159.203.182.9:8081` → log in with the Demo bootstrap user.

### Useful non-destructive operations

```bash
cd /opt/keys
docker compose -f docker-compose.demo.yml --env-file .env.demo logs -f web
docker compose -f docker-compose.demo.yml --env-file .env.demo up -d --build
docker compose -f docker-compose.demo.yml --env-file .env.demo down
```

`down` removes only KeyInventory compose containers/network links; it does **not** stop DentalInventory SQL or remove SQL volumes.

---

## RESET / RESEED DEMO

**Destructive.** Explicitly destroys **only** `KeyInventoryDemo`, then recreates schema through the existing migrate service and recreates/reconciles `LocalBootstrapAdmin` on Web startup. Business data is left **empty**.

### Repository-owned reset mechanism

| Item | Value |
| --- | --- |
| Script | `docker/reset-keyinventory-demo.sh` |
| Destructive target | Hard-coded `KeyInventoryDemo` (no database-name argument) |
| Schema recreation | Existing `migrate` service → `dotnet ef database update` |
| Login reseed | Existing `LocalBootstrapAdmin` on Web startup |
| Business seed | **None** |

The script fails closed unless Demo invariants are satisfied. It never deletes SQL volumes, never recreates the SQL Server container, and never drops any database other than `KeyInventoryDemo`.

Reset is **not** exposed through the Web application and is **not** part of normal `up -d`.

### Exact Droplet commands (after this repository change is on `origin/master`)

```bash
cd /opt/keys
git fetch origin
git checkout master
git reset --hard origin/master
test -f .env.demo
chmod 600 .env.demo
chmod +x docker/reset-keyinventory-demo.sh

# Stop Web so it is not holding KeyInventoryDemo connections.
docker compose -f docker-compose.demo.yml --env-file .env.demo stop web

# Explicit destructive reset — KeyInventoryDemo only.
./docker/reset-keyinventory-demo.sh

# Rebuild images from accepted source, recreate schema, start Web.
docker compose -f docker-compose.demo.yml --env-file .env.demo build
docker compose -f docker-compose.demo.yml --env-file .env.demo run --rm migrate
docker compose -f docker-compose.demo.yml --env-file .env.demo up -d --force-recreate --no-deps web

# Health
for i in $(seq 1 40); do
  curl -fsS http://127.0.0.1:8081/health/ready && break
  sleep 3
done
curl -fsS http://127.0.0.1:8081/health/ready
echo
```

`run --rm migrate` must exit 0. KEY-ACCESS-COPY-1 applies on the empty recreated database; its legacy-data STOP behavior for non-empty catalog/loan rows is unchanged and is not used on a successful demo reset (database starts empty).
