#!/bin/bash
set -e

if [ -z "$KRAFT_DEPLOY_LOCKED" ]; then
    exec 9> /tmp/kraft-deploy.lock
    flock -n 9 || exit 0
    export KRAFT_DEPLOY_LOCKED=1
fi

if docker-compose version &>/dev/null 2>&1; then
    COMPOSE="docker-compose"
elif docker compose version &>/dev/null 2>&1; then
    COMPOSE="docker compose"
else
    echo "Error: docker compose not found" >&2
    exit 1
fi

cd "$(dirname "$0")"

DEPLOYED_COMMIT_FILE=".last-deployed-commit"

if [ "$1" = "--build" ]; then
    echo "$(date): Building and starting containers..."
    $COMPOSE build api
    $COMPOSE build web
    $COMPOSE up -d api web
    git rev-parse HEAD > "$DEPLOYED_COMMIT_FILE"
    docker image prune -f > /dev/null
    echo "$(date): Deploy complete"
    exit 0
fi
git fetch origin main
REMOTE=$(git rev-parse origin/main)
DEPLOYED=$(cat "$DEPLOYED_COMMIT_FILE" 2>/dev/null || true)
if [ "$DEPLOYED" != "$REMOTE" ]; then
    echo "$(date): Changes detected, deploying..."
    git reset --hard origin/main
    git clean -fd -e "$DEPLOYED_COMMIT_FILE"
    exec "$0" --build
fi
