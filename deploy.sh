#!/bin/bash

# Configuration
PROJECT_DIR="/home/durgle/TestDataServer"
TEMP_DIR="/tmp/TestDataServer_build"
OUTPUT_DIR="/opt/TestDataServerV0"

echo "Starting TestDataServer publishing script..."

if [ "$EUID" -ne 0 ]; then
	echo "Re-run script with root privileges"
	exit 1
fi

echo "Publishing project..."

dotnet publish -c Release --artifacts-path "$TEMP_DIR"

sudo rm -rf "$OUTPUT_DIR"/*
sudo cp -r "$TEMP_DIR"/* "$OUTPUT_DIR"
rm -rf "$TEMP_PUBLISH_DIR"

sudo chown -R testserveruser:durgle "$OUTPUT_DIR"
sudo chmod -R 755 "${OUTPUT_DIR:?error message}"

echo "Reloading daemons and restarting TestDataServer..."

sudo systemctl daemon-reload
sudo systemctl restart TestDataServer

echo "Script complete.  Project published to: $OUTPUT_DIR"
