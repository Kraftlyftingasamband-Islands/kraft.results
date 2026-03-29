#!/bin/bash
set -e
cd /mnt/user/appdata/kraft-src
git fetch origin main
LOCAL=$(git rev-parse main)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git reset --hard origin/main
    git clean -fd
    docker-compose build api web
    docker-compose up -d api web
    echo "$(date): Deploy complete"
fi
