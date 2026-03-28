#!/bin/bash
set -e
cd /root/kraft.results
git fetch origin main
LOCAL=$(git rev-parse main)
REMOTE=$(git rev-parse origin/main)
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git checkout main
    git pull origin main
    docker-compose build api web
    docker-compose up -d api web
    echo "$(date): Deploy complete"
fi
