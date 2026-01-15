#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

PROJECT_DIR="$SCRIPT_DIR"
PROJECT_FILE="TestDataServer.csproj"
OUTPUT_DIR="/opt/TestDataServerV0"
TEMP_DIR="/tmp/TestDataServer_publish"

echo "Starting TestDataServer deployment..."

if [[ $EUID -ne 0 ]]; then
  echo "Please run as root"
  exit 1
fi

echo "Cleaning temp directory..."
rm -rf "$TEMP_DIR"
mkdir -p "$TEMP_DIR"

echo "Publishing project..."
dotnet publish "$PROJECT_DIR/$PROJECT_FILE" \
  -c Release \
  -o "$TEMP_DIR"

echo "Stopping service..."
systemctl stop TestDataServer

echo "Deploying files..."
rm -rf "${OUTPUT_DIR:?}/"*
cp -a "$TEMP_DIR"/. "$OUTPUT_DIR"/

echo "Fixing ownership and permissions..."
chown -R testserveruser:testserveruser "$OUTPUT_DIR"
chmod -R 755 "$OUTPUT_DIR"

echo "Starting service..."
systemctl start TestDataServer

echo "Deployment complete."
