#!/usr/bin/env bash
set -eu

base_url="${BASE_URL:-http://localhost:5000}"
base_url="${base_url%/}"

check_endpoint() {
    path="$1"
    url="$base_url$path"
    if ! status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' "$url")"; then
        printf 'Request failed: %s\n' "$url" >&2
        return 1
    fi
    if [ "$status" != "200" ]; then
        printf 'Unexpected HTTP status %s from %s (expected 200).\n' "$status" "$url" >&2
        return 1
    fi
    printf 'OK %s\n' "$url"
}

check_endpoint "/health"
check_endpoint "/api/tasks"
check_endpoint "/api/users"
