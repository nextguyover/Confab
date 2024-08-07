FROM mcr.microsoft.com/dotnet/aspnet:6.0.32-alpine3.20-amd64

WORKDIR /confab

COPY App/ ./
COPY App/appsettings.json /confab-config/appsettings.json
COPY scripts/docker_prestart.sh /confab/docker_prestart.sh

VOLUME ["/confab/Database", "/confab-config"]

EXPOSE 2632

CMD sh /confab/docker_prestart.sh