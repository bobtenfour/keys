#!/usr/bin/env bash
# DEMO / EVALUATION ONLY — complete clean rebuild of KeyInventory demo on the Droplet.
# Authorized destructive scope (fixed):
#   - keyinventory-demo containers/images
#   - database KeyInventoryDemo only
#   - directory /opt/keys only (recreated by git clone)
# Preserves: dentalinventory-demo-sql/web, SQL volumes, DentalInventoryDemo, KeyInventoryDev,
#            master/model/msdb/tempdb, every other database, shared network.
# Does NOT accept database-name arguments. Does NOT use compose down -v, SQL volume deletion, or host-wide prune.
# Schema: existing migrate service. Login: LocalBootstrapAdmin. Business data: empty.
set -euo pipefail

readonly INSTALL_DIR="/opt/keys"
readonly REPO_URL="https://github.com/bobtenfour/keys.git"
readonly REPO_BRANCH="master"
readonly COMPOSE_FILE="docker-compose.demo.yml"
readonly ENV_FILE=".env.demo"
readonly SQL_CONTAINER="dentalinventory-demo-sql"
readonly NETWORK="dentalinventory-demo_default"
readonly TARGET_DATABASE="KeyInventoryDemo"
readonly ENV_BACKUP="/tmp/keyinventory-demo.env.demo.bak"
readonly SELF_RUNTIME="/tmp/clean-rebuild-keyinventory-demo.sh"

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

if [[ "${#}" -ne 0 ]]; then
  fail "This script accepts no arguments. Destructive targets are fixed."
fi

if [[ "${TARGET_DATABASE}" != "KeyInventoryDemo" ]]; then
  fail "Internal invariant failed: TARGET_DATABASE must be KeyInventoryDemo."
fi

if [[ "${INSTALL_DIR}" != "/opt/keys" ]]; then
  fail "Internal invariant failed: INSTALL_DIR must be /opt/keys."
fi

for forbidden in "${FORBIDDEN_DATABASES[@]}"; do
  if [[ "${TARGET_DATABASE}" == "${forbidden}" ]]; then
    fail "Refusing clean rebuild: target collides with protected database ${forbidden}."
  fi
done

# Re-exec from /tmp so rm -rf /opt/keys cannot truncate this script mid-run.
SCRIPT_SOURCE="$(readlink -f "${BASH_SOURCE[0]}" 2>/dev/null || realpath "${BASH_SOURCE[0]}" 2>/dev/null || echo "${BASH_SOURCE[0]}")"
if [[ "${SCRIPT_SOURCE}" != "${SELF_RUNTIME}" ]]; then
  case "${SCRIPT_SOURCE}" in
    "${INSTALL_DIR}"/*) ;;
    *)
      case "${PWD}" in
        "${INSTALL_DIR}"|"${INSTALL_DIR}"/*) ;;
        *)
          fail "Clean rebuild may run only on the Droplet under ${INSTALL_DIR} (refusing path '${SCRIPT_SOURCE}' pwd='${PWD}')."
          ;;
      esac
      ;;
  esac
  cp -f "${SCRIPT_SOURCE}" "${SELF_RUNTIME}"
  chmod 700 "${SELF_RUNTIME}"
  exec "${SELF_RUNTIME}"
fi

# Runtime copy is executing; require Droplet install path still present for pre-wipe steps.
if [[ ! -d "${INSTALL_DIR}" ]]; then
  fail "${INSTALL_DIR} is missing before clean rebuild."
fi

cd "${INSTALL_DIR}"
test -f "${ENV_FILE}" || fail "${INSTALL_DIR}/${ENV_FILE} is required (will be backed up then restored after clone)."
test -f "${COMPOSE_FILE}" || fail "${INSTALL_DIR}/${COMPOSE_FILE} is required."

cp -a "${ENV_FILE}" "${ENV_BACKUP}"
chmod 600 "${ENV_BACKUP}"

set -a
# shellcheck disable=SC1091
source "${ENV_FILE}"
set +a
[[ -n "${MSSQL_SA_PASSWORD:-}" ]] || fail "MSSQL_SA_PASSWORD is required in ${ENV_FILE}."
[[ -n "${LocalBootstrapAdmin__Password:-}" ]] || fail "LocalBootstrapAdmin__Password is required in ${ENV_FILE}."

if [[ -n "${ASPNETCORE_ENVIRONMENT:-}" && "${ASPNETCORE_ENVIRONMENT}" != "Demo" ]]; then
  fail "Clean rebuild is Demo-only. ASPNETCORE_ENVIRONMENT must be Demo when set."
fi

CONN="${ConnectionStrings__KeyInventory:-}"
if [[ -n "${CONN}" ]]; then
  echo "${CONN}" | grep -Eqi "DentalInventoryDemo|DentalInventoryDev" \
    && fail "Connection string must not reference DentalInventory databases."
  echo "${CONN}" | grep -Eqi "Database=KeyInventoryDev(;|$)" \
    && fail "Connection string must not target KeyInventoryDev."
  echo "${CONN}" | grep -Eqi "Database=KeyInventoryDemo(;|$)" \
    || fail "Connection string Database= must be KeyInventoryDemo only."
fi

docker ps --format '{{.Names}}' | grep -Fxq "${SQL_CONTAINER}" \
  || fail "Shared SQL container '${SQL_CONTAINER}' is not running. Do not recreate SQL here."
docker network inspect "${NETWORK}" >/dev/null \
  || fail "Shared Docker network '${NETWORK}' is missing."

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

echo "=== 1) Verify current KeyInventory containers ==="
docker ps -a --format '{{.Names}} {{.Status}}' | grep -E '^keyinventory-demo-' || true

echo "=== Databases BEFORE ==="
run_sql "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"

echo "=== 2) Stop/remove ONLY keyinventory-demo containers (no volume wipe) ==="
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" stop || true
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" rm -f || true
docker rm -f keyinventory-demo-web keyinventory-demo-migrate 2>/dev/null || true

echo "=== Remove ONLY keyinventory-demo images (never prune the host Docker store) ==="
docker images --format '{{.Repository}}:{{.Tag}} {{.ID}}' \
  | awk '/keyinventory-demo/ { print $2 }' \
  | sort -u \
  | while read -r img; do
      docker rmi -f "${img}" 2>/dev/null || true
    done

echo "=== 3) Drop ONLY KeyInventoryDemo ==="
EXISTS="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'KeyInventoryDemo') IS NULL THEN N'MISSING' ELSE N'PRESENT' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
echo "KeyInventoryDemo=${EXISTS}"
if [[ "${EXISTS}" == "PRESENT" ]]; then
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
  echo "KeyInventoryDemo already absent."
else
  fail "Could not determine KeyInventoryDemo existence (got '${EXISTS}')."
fi
GONE="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'KeyInventoryDemo') IS NULL THEN N'GONE' ELSE N'STILL_PRESENT' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
[[ "${GONE}" == "GONE" ]] || fail "KeyInventoryDemo still present after DROP."

echo "=== Databases AFTER DROP ==="
run_sql "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"

echo "=== 4) Remove ONLY ${INSTALL_DIR} ==="
cd /
rm -rf "${INSTALL_DIR}"
[[ ! -e "${INSTALL_DIR}" ]] || fail "${INSTALL_DIR} still exists after removal."

echo "=== 5) Clone accepted master to ${INSTALL_DIR} ==="
git clone --branch "${REPO_BRANCH}" --single-branch "${REPO_URL}" "${INSTALL_DIR}"
cd "${INSTALL_DIR}"
git rev-parse --abbrev-ref HEAD | grep -Fxq "${REPO_BRANCH}" \
  || fail "Clone is not on branch ${REPO_BRANCH}."
echo "HEAD=$(git rev-parse HEAD)"

echo "=== 6) Restore .env.demo safely ==="
cp -a "${ENV_BACKUP}" "${INSTALL_DIR}/${ENV_FILE}"
chmod 600 "${INSTALL_DIR}/${ENV_FILE}"
set -a
# shellcheck disable=SC1091
source "${INSTALL_DIR}/${ENV_FILE}"
set +a
[[ -n "${MSSQL_SA_PASSWORD:-}" ]] || fail "Restored ${ENV_FILE} missing MSSQL_SA_PASSWORD."
[[ -n "${LocalBootstrapAdmin__Password:-}" ]] || fail "Restored ${ENV_FILE} missing LocalBootstrapAdmin__Password."
chmod +x "${INSTALL_DIR}/docker/reset-keyinventory-demo.sh" 2>/dev/null || true
chmod +x "${INSTALL_DIR}/docker/clean-rebuild-keyinventory-demo.sh" 2>/dev/null || true

echo "=== 7) Build migrate + web from scratch ==="
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" build --no-cache

echo "=== 8) Migrate clean KeyInventoryDemo ==="
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" run --rm migrate

echo "=== 9) Start web ==="
docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d --force-recreate --no-deps web

echo "=== 10) Migrate exit 0 enforced by set -e ==="

echo "=== 11–12) Web healthy + /health/ready ==="
for i in $(seq 1 40); do
  if curl -fsS http://127.0.0.1:8081/health/ready >/dev/null; then
    break
  fi
  if [[ "${i}" -eq 40 ]]; then
    docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" logs --tail=100 web || true
    fail "Web did not become healthy."
  fi
  sleep 3
done
curl -fsS http://127.0.0.1:8081/health/ready
echo

echo "=== 13) DentalInventory containers remain running ==="
docker ps --format '{{.Names}}' | grep -Fxq dentalinventory-demo-web \
  || fail "dentalinventory-demo-web is not running."
docker ps --format '{{.Names}}' | grep -Fxq dentalinventory-demo-sql \
  || fail "dentalinventory-demo-sql is not running."
docker ps --format '{{.Names}} {{.Status}}' | grep -E 'dentalinventory-demo-web|dentalinventory-demo-sql'

echo "=== 14) Other databases remain; KeyInventoryDemo recreated ==="
run_sql "SET NOCOUNT ON; SELECT name FROM sys.databases ORDER BY name;"
DENTAL_OK="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'DentalInventoryDemo') IS NOT NULL THEN N'OK' ELSE N'MISSING' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
[[ "${DENTAL_OK}" == "OK" ]] || fail "DentalInventoryDemo missing after clean rebuild."
KI_OK="$(run_sql "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'KeyInventoryDemo') IS NOT NULL THEN N'OK' ELSE N'MISSING' END;" \
  | tr -d '\r' | awk 'NF { print $1 }' | tail -n 1)"
[[ "${KI_OK}" == "OK" ]] || fail "KeyInventoryDemo was not recreated by migrate."

echo "=== KeyInventory containers ==="
docker ps -a --format '{{.Names}} {{.Status}}' | grep -E '^keyinventory-demo-' || true

echo "CLEAN_REBUILD_COMPLETE"
echo "Public: http://159.203.182.9:8081"
echo "Business data is empty; LocalBootstrapAdmin login comes from restored .env.demo."
