#!/bin/bash

printf "                                                                                 \n  ,ad8888ba,    ,ad8888ba,    888b      88  88888888888  db         88888888ba   \n d8\"\'    \`\"8b  d8\"\'    \`\"8b   8888b     88  88          d88b        88      \"8b  \nd8\'           d8\'        \`8b  88 \`8b    88  88         d8\'\`8b       88      ,8P  \n88            88          88  88  \`8b   88  88aaaaa   d8\'  \`8b      88aaaaaa8P\'  \n88            88          88  88   \`8b  88  88\"\"\"\"\"  d8YaaaaY8b     88\"\"\"\"\"\"8b,  \nY8,           Y8,        ,8P  88    \`8b 88  88      d8\"\"\"\"\"\"\"\"8b    88      \`8b  \n Y8a.    .a8P  Y8a.    .a8P   88     \`8888  88     d8\'        \`8b   88      a8P  \n  \`\"Y8888Y\"\'    \`\"Y8888Y\"\'    88      \`888  88    d8\'          \`8b  88888888P\"   \n                                                                                 \n"

printf "## --------- Beautiful comments for your site - confabcomments.com --------- ##\n\n\n"


printf "Welcome to the Confab installer!\n\n"

printf "This script will install/update Confab on your system and setup a systemd\n\
service. Please note that only linux-x64 is currently supported. For more\n\
info, see docs.confabcomments.com.\n\n"

printf "THIS SCRIPT REQURIES ELEVATED PRIVILAGES: sudo is required for install and\n\
to stop/start systemd service.\n"

install_dir=/opt/confab
tmp_dir=/tmp/confab

printf "\nReady to install/update Confab at directory \`${install_dir}\`. Any non-Confab\n\
files here WILL BE DELETED. Press Enter to continue..."
read

printf "\nDownloading Confab release package...\n"

rm -rf ${tmp_dir}
package_download_url=$(curl -s https://api.github.com/repos/nextguyover/Confab/releases/latest | grep -wo "https:\/\/github.com\/nextguyover\/Confab\/.*Confab.*-linux-x64.zip")
wget -q --show-progress --content-disposition -P ${tmp_dir}/download ${package_download_url} 2>&1

if [ $? -ne 0 ] 
then 
    printf "\nDownload failed :(\n"
    exit
fi

download_filename=$(find ${tmp_dir}/download -type f -name "*.zip" -print -quit)

printf "\nExtracting package... "
unzip -q ${download_filename} -d ${tmp_dir}/extracted
printf "done!\n\n"

if [ -f "${install_dir}/autostart-linux.sh" ]; then
    printf "Stopping Confab service (if running)... "
    sudo bash ${install_dir}/autostart-linux.sh --disable
fi

printf "Copying Confab v$(cat ${tmp_dir}/extracted/version) to \`${install_dir}\`..."

# Delete all files in ${install_dir}/ except for appsettings.json, jwt-key and Database/*
find ${install_dir}/ -type f ! -name "appsettings.json" ! -name "jwt-key" ! -path "${install_dir}/Database/*" -delete

rsync -a --ignore-times --exclude='appsettings.json' --exclude='jwt-key' --exclude='Database/*' ${tmp_dir}/extracted/* ${install_dir}/
rsync -a --ignore-existing ${tmp_dir}/extracted/appsettings.json ${install_dir}/appsettings.json

printf " done!\n\n"

printf "Starting Confab service... "
sudo bash ${install_dir}/autostart-linux.sh --enable

printf "\nInstallation/update completed successfully! Cleaning up..."
rm -rf ${tmp_dir}
printf " done!\n"

printf "Next, setup your Confab server by following instructions here:\n\
https://docs.confabcomments.com/quick-start/#server-setup\n"
