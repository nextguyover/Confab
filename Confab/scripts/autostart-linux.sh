#!/bin/bash

echo ""

display_usage() {
    echo "Enable or disabled Confab autostart via systemd service. Confab excecutable must be in the same directory as this script."
    echo "Usage: $0 [--enable|--disable]"
    echo ""
    echo "This script must be run with elevated privileges (sudo)"
}

if [ "$#" -ne 1 ]; then
    display_usage
    echo ""
    exit 1
fi

if [ "$EUID" -ne 0 ]; then
    display_usage
    echo ""
    exit 1
fi

SERVICE_NAME="confab"
SERVICE_DESCRIPTION="Confab comments backend service"

EXECUTABLE_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
EXECUTABLE_PATH="$EXECUTABLE_DIR/Confab"

SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"

case "$1" in
    --enable)
        echo "Enabling autostart..."
        
        echo "[Unit]" > "$SERVICE_FILE"
        echo "Description=$SERVICE_DESCRIPTION" >> "$SERVICE_FILE"
        echo "After=network.target" >> "$SERVICE_FILE"
        echo "" >> "$SERVICE_FILE"
        echo "[Service]" >> "$SERVICE_FILE"
        echo "ExecStart=$EXECUTABLE_PATH" >> "$SERVICE_FILE"
        echo "WorkingDirectory=$EXECUTABLE_DIR" >> "$SERVICE_FILE"
        echo "Type=notify" >> "$SERVICE_FILE"
        echo "Restart=always" >> "$SERVICE_FILE"
        echo "" >> "$SERVICE_FILE"
        echo "[Install]" >> "$SERVICE_FILE"
        echo "WantedBy=default.target" >> "$SERVICE_FILE"

        echo "Created systemd service at $SERVICE_FILE"

        echo "Reloading systemd"
        sudo systemctl daemon-reload

        echo "Enabling and starting systemd service $SERVICE_NAME"
        sudo systemctl enable $SERVICE_NAME
        sudo systemctl start $SERVICE_NAME

        echo "Systemd service '$SERVICE_NAME' has been created, enabled, and started."
        echo "Check '$SERVICE_NAME' status using 'systemctl status confab'"
        echo "View logs using 'journalctl -fe _SYSTEMD_UNIT=$SERVICE_NAME.service'"
        
        echo ""
        exit 0
        ;;
    --disable)
        echo "Disabling autostart..."

        echo "Stopping systemd service $SERVICE_NAME"
        sudo systemctl stop $SERVICE_NAME

        echo "Deleting $SERVICE_FILE"
        rm $SERVICE_FILE

        echo "Cleaning up"
        sudo systemctl reset-failed $SERVICE_NAME
        sudo systemctl daemon-reload

        echo "Succesfully disabled autostart"
        echo ""
        exit 0
        ;;
    --help)
        display_usage
        echo ""
        exit 0
        ;;
    *)
        echo "Invalid option: $1. Use --enable or --disable. Use --help for more info."
        echo ""
        exit 1
        ;;
esac


