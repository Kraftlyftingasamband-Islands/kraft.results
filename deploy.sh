#!/bin/bash
set -e

if command -v docker-compose &>/dev/null; then
    COMPOSE="docker-compose"
else
    COMPOSE="docker compose"
fi
cd /mnt/user/appdata/kraft-src
git fetch origin main
LOCAL=$(git rev-parse main)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git reset --hard origin/main
    git clean -fd
    $COMPOSE build api
    $COMPOSE build web
    $COMPOSE up -d api web
    echo "$(date): Deploy complete"
fi
