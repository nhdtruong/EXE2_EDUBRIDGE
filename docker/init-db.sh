#!/usr/bin/env bash
set -euo pipefail

DB_HOST="${DB_HOST:-sqlserver}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-EduBridgeDB}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
ENABLE_MOCK_SEED="${ENABLE_MOCK_SEED:-false}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
ROOT_DIR="/work/edubridge_database"

wait_for_sql() {
  echo "Waiting for SQL Server at ${DB_HOST}:${DB_PORT}..."
  for _ in $(seq 1 90); do
    if "${SQLCMD}" -S "${DB_HOST},${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWORD}" -C -Q "SELECT 1" >/dev/null 2>&1; then
      echo "SQL Server is ready."
      return 0
    fi
    sleep 2
  done

  echo "SQL Server did not become ready in time." >&2
  return 1
}

run_sql_file() {
  local file="$1"
  echo "Applying ${file}"
  "${SQLCMD}" -S "${DB_HOST},${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWORD}" -C -b -f 65001 -i "${file}"
}

run_sql_query() {
  local query="$1"
  "${SQLCMD}" -S "${DB_HOST},${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWORD}" -C -b -h -1 -W -Q "${query}"
}

ensure_database() {
  local exists
  exists="$(run_sql_query "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'${DB_NAME}') IS NULL THEN 0 ELSE 1 END;")"
  exists="$(echo "${exists}" | tr -d '\r\n[:space:]')"

  if [[ "${exists}" != "1" ]]; then
    run_sql_file "${ROOT_DIR}/init/00_create_database.sql"
  fi
}

ensure_migration_table() {
  run_sql_query "USE [${DB_NAME}];
IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__SchemaMigrations
    (
        ScriptName NVARCHAR(255) NOT NULL PRIMARY KEY,
        AppliedAtUtc DATETIME2 NOT NULL CONSTRAINT DF___SchemaMigrations_AppliedAtUtc DEFAULT SYSUTCDATETIME()
    );
END"
}

apply_script_once() {
  local file="$1"
  local name
  name="$(basename "${file}")"

  local applied
  applied="$(run_sql_query "USE [${DB_NAME}]; SET NOCOUNT ON; SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.__SchemaMigrations WHERE ScriptName = N'${name}') THEN 1 ELSE 0 END;")"
  applied="$(echo "${applied}" | tr -d '\r\n[:space:]')"

  if [[ "${applied}" == "1" ]]; then
    echo "Skipping ${name}; already applied."
    return 0
  fi

  run_sql_file "${file}"
  run_sql_query "USE [${DB_NAME}]; INSERT INTO dbo.__SchemaMigrations (ScriptName) VALUES (N'${name}');"
}

main() {
  wait_for_sql
  ensure_database
  ensure_migration_table

  while IFS= read -r -d '' file; do
    apply_script_once "${file}"
  done < <(find "${ROOT_DIR}/migration" -maxdepth 1 -type f -name '*.sql' | sort -z)

  if [[ "${ENABLE_MOCK_SEED,,}" == "true" ]]; then
    apply_script_once "${ROOT_DIR}/seed/parent_app_mock.sql"
  else
    echo "Mock seed disabled."
  fi

  echo "Database initialization completed."
}

main "$@"
