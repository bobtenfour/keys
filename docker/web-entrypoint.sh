#!/bin/sh
# DEMO ONLY - validate runtime secrets, bind PORT, start web (no migrations here).
set -eu

if [ -z "${LocalBootstrapAdmin__Password:-}" ]; then
  echo "ERROR: LocalBootstrapAdmin__Password is required for the Docker demo. Set it in .env.demo."
  exit 1
fi

if [ -z "${LocalBootstrapAdmin__UserName:-}" ]; then
  echo "ERROR: LocalBootstrapAdmin__UserName is required for the Docker demo."
  exit 1
fi

if [ -z "${ConnectionStrings__KeyInventory:-}" ]; then
  echo "ERROR: ConnectionStrings__KeyInventory is required."
  exit 1
fi

if echo "${ConnectionStrings__KeyInventory}" | grep -Eqi "DentalInventoryDemo|DentalInventoryDev"; then
  echo "ERROR: KeyInventory demo web must not reference DentalInventory databases."
  exit 1
fi

if ! echo "${ConnectionStrings__KeyInventory}" | grep -Eqi "Database=KeyInventoryDemo(;|$)"; then
  echo "ERROR: KeyInventory demo web must target Database=KeyInventoryDemo only."
  exit 1
fi

export ASPNETCORE_URLS="http://+:${PORT:-8080}"

exec dotnet KeyInventory.Web.dll
