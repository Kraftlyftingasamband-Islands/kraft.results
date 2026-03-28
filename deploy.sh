#!/usr/bin/env bash
set -euo pipefail

TOWER_HOST="${TOWER_HOST:-tower}"
TOWER_USER="${TOWER_USER:-$(whoami)}"
TOWER_REPO="${TOWER_REPO:-/home/$TOWER_USER/kraft.results}"

echo "Deploying to $TOWER_USER@$TOWER_HOST:$TOWER_REPO..."

ssh "$TOWER_USER@$TOWER_HOST" bash -s <<EOF
set -euo pipefail
cd "$TOWER_REPO"
git pull
docker compose up -d --build
EOF

echo "Deployed successfully."
