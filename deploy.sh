#!/bin/bash
set -e

if command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    COMPOSE="docker compose"
fi

if [ "$1" = "--build" ]; then
    echo "$(date): Building and starting containers..."
    $COMPOSE build api
    $COMPOSE build web
    $COMPOSE up -d api web
    echo "$(date): Deploy complete"
    exit 0
fi

cd /mnt/user/appdata/kraft-src
git fetch origin main
LOCAL=$(git rev-parse main)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git reset --hard origin/main
    git clean -fd
    exec "$0" --build
fi
