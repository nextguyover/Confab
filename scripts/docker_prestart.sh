#!/bin/bash

# This script is executed before the application starts inside the Docker 
# container. 

# Copy the appsettings.json configuration file from the docker volume to the 
# Confab executable directory.
cp /confab-config/appsettings.json /confab/appsettings.json

# Start the Confab application.
/confab/Confab