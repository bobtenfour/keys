#!/usr/bin/env bash
# DEMO / EVALUATION ONLY — explicit KeyInventoryDemo reset (destructive).
# Drops ONLY the hard-coded disposable database KeyInventoryDemo on the shared SQL Server.
# Does NOT migrate, start Web, delete volumes, recreate SQL, or accept a database-name argument.
# Schema recreation: existing compose migrate service (dotnet ef database update).
# Login reseed: existing LocalBootstrapAdmin on Web startup. Business data remains empty.
set -euo pipefail

readonly TARGET_DATABASE="KeyInventoryDemo"
readonly FORBIDDEN_DATABASES=(
  "KeyInventoryDev"
  "DentalInventoryDemo"
  "DentalInventoryDev"
  "master"
  "model"
  "msdb"
  "tempdb"
)

fail() {
  echo "ERROR: $*" >&2
  exit 1
}

# No database-name parameters — target is fixed.
if [[ "${#}" -ne 0 ]]; then
  fail "This script accepts no arguments. Destructive target is fixed to ${TARGET_DATABASE}."
fi

# Fail closed: never allow TARGET_DATABASE to drift from KeyInventoryDemo.
if [[ "${TARGET_DATABASE}" != "KeyInventoryDemo" ]]; then
  fail "Internal invariant failed: TARGET_DATABASE must be KeyInventoryDemo."
fi

for forbidden in "${FORBIDDEN_DATABASES[@]}"; do
  if [[ "${TARGET_DATABASE}" == "${forbidden}" ]]; then
    fail "Refusing destructive reset: target collides with protected database ${forbidden}."
  fi
done

# Demo-only operational context (Droplet /opt/keys with .env.demo).
if [[ -f .env.demo ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env.demo
  set +a
elif [[ -f ../.env.demo ]]; then
  set -a
  # shellcheck disable=SC1091
  source ../.env.demo
  set +a
fi

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
  fail "MSSQL_SA_PASSWORD is required (set in .env.demo or the environment)."
fi

# Optional Demo environment gate when ASPNETCORE_ENVIRONMENT is present.
if [[ -n "${ASPNETCORE_ENVIRONMENT:-}" && "${ASPNETCORE_ENVIRONMENT}" != "Demo" ]]; then
  fail "Reset is Demo-only. ASPNETCORE_ENVIRONMENT must be Demo when set (was '${ASPNETCORE_ENVIRONMENT}')."
fi

# Connection string, if provided, must target KeyInventoryDemo only and must not name protected DBs as the Database=.
CONN="${ConnectionStrings__KeyInventory:-}"
if [[ -n "${CONN}" ]]; then
  if echo "${CONN}" | grep -Eqi "DentalInventoryDemo|DentalInventoryDev"; then
    fail "Connection string must not reference DentalInventory databases."
  fi
  if echo "${CONN}" | grep -Eqi "Database=KeyInventoryDev(;|$)"; then
    fail "Connection string must not target KeyInventoryDev."
  fi
  if ! echo "${CONN}" | grep -Eqi "Database=KeyInventoryDemo(;|$)"; then
    fail "Connection string Database= must be KeyInventoryDemo only."
  fi
fi

SQL_CONTAINER="${DEMO_SQL_CONTAINER:-dentalinventory-demo-sql}"
if ! docker ps --format '{{.Names}}' | grep -Fxq "${SQL_CONTAINER}"; then
  fail "Shared SQL container '${SQL_CONTAINER}' is not running. Do not create or recreate SQL here."
fi

if docker exec "${SQL_CONTAINER}" test -x /opt/mssql-tools18/bin/sqlcmd; then
  SQLCMD=/opt/mssql-tools18/bin/sqlcmd
  SQLCMD_TRUST=(-C)
elif docker exec "${SQL_CONTAINER}" test -x /opt/mssql-tools/bin/sqlcmd; then
  SQLCMD=/opt/mssql-tools/bin/sqlcmd
  SQLCMD_TRUST=()
else
  fail "sqlcmd not found in ${SQL_CONTAINER}."
fi

run_sql() {
  docker exec "${SQL_CONTAINER}" "${SQLCMD}" \
    -S localhost \
    -U sa \
    -P "${MSSQL_SA_PASSWORD}" \
    "${SQLCMD_TRUST[@]}" \
    -b \
    -Q "$1"
}

echo "=== KeyInventoryDemo reset (destructive, Demo only) ==="
echo "SQL container: ${SQL_CONTAINER}"
echo "Target database (fixed): ${TARGET_DATABASE}"
echo "=== Databases before ==="
run_sql "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"

EXISTS="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'KeyInventoryDemo') IS NULL THEN N'MISSING' ELSE N'PRESENT' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
echo "KeyInventoryDemo status: ${EXISTS}"

if [[ "${EXISTS}" == "PRESENT" ]]; then
  # Terminate connections to KeyInventoryDemo only, then DROP only that database.
  run_sql "
IF DB_ID(N'KeyInventoryDemo') IS NULL
BEGIN
  RAISERROR('Refusing DROP: KeyInventoryDemo does not exist.', 16, 1);
END
ELSE
BEGIN
  ALTER DATABASE [KeyInventoryDemo] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [KeyInventoryDemo];
END
"
elif [[ "${EXISTS}" == "MISSING" ]]; then
  echo "KeyInventoryDemo already absent; nothing to drop."
else
  fail "Could not determine KeyInventoryDemo existence (got '${EXISTS}')."
fi

GONE="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'KeyInventoryDemo') IS NULL THEN N'GONE' ELSE N'STILL_PRESENT' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
echo "After drop: ${GONE}"
[[ "${GONE}" == "GONE" ]] || fail "KeyInventoryDemo still present after DROP."

echo "=== Databases after ==="
run_sql "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"

echo "RESET_OK: ${TARGET_DATABASE} dropped (or already absent)."
echo "Next: recreate schema with compose migrate, then start web (LocalBootstrapAdmin only; empty business data)."
echo "  docker compose -f docker-compose.demo.yml --env-file .env.demo run --rm migrate"
echo "  docker compose -f docker-compose.demo.yml --env-file .env.demo up -d --force-recreate --no-deps web"
