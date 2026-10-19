#!/usr/bin/env bash
set -eu

script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
repo_root="$(dirname "$script_dir")"
database="$repo_root/src/TaskFlow.Api/data/taskflow.db"
api_project="$repo_root/src/TaskFlow.Api/TaskFlow.Api.csproj"

cd "$repo_root"
mkdir -p "$(dirname "$database")"
rm -f -- "$database" "$database-wal" "$database-shm"

if ! ASPNETCORE_ENVIRONMENT=Development dotnet run \
    --project "$api_project" \
    --no-launch-profile \
    -- \
    --initialize-database; then
    printf 'Database reset failed; migration or seed initialization did not complete.\n' >&2
    exit 1
fi

printf 'Development database reset and seeded: %s\n' "$database"
