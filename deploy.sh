#!/bin/bash
set -e

if docker-compose version &>/dev/null 2>&1; then
    COMPOSE="docker-compose"
elif docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
else
    echo "Error: docker compose not found" >&2
    exit 1
fi

cd "$(dirname "$0")"

if [ "$1" = "--build" ]; then
    echo "$(date): Building and starting containers..."
    $COMPOSE build api
    $COMPOSE build web
    $COMPOSE up -d api web
    echo "$(date): Deploy complete"
    exit 0
fi
git fetch origin main
LOCAL=$(git rev-parse main)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git reset --hard origin/main
    git clean -fd
    exec "$0" --build
fi
