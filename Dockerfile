# STAGE 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copie du projet + restauration des dépendances
COPY ["RootBackend.csproj", "."]
RUN dotnet restore "RootBackend.csproj"

# Copie du reste + build
COPY . .
RUN dotnet build "RootBackend.csproj" -c Release -o /app/build

# Publication de l'application
RUN dotnet publish "RootBackend.csproj" -c Release -o /app/publish

# Installation Playwright CLI
RUN dotnet tool install --global Microsoft.Playwright.CLI
# Ajout de Playwright au PATH
ENV PATH="$PATH:/root/.dotnet/tools"
# Installation des navigateurs Playwright
# On reste dans /src où se trouve le projet et pas dans /app/publish
RUN playwright install --with-deps chromium

# STAGE 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Dépendances Linux requises par Playwright (headless Chromium)
RUN apt-get update && apt-get install -y \
    wget gnupg ca-certificates fonts-liberation \
    libappindicator3-1 libasound2 libatk-bridge2.0-0 libatk1.0-0 libcups2 \
    libdbus-1-3 libgdk-pixbuf2.0-0 libnspr4 libnss3 libx11-xcb1 \
    libxcomposite1 libxdamage1 libxrandr2 xdg-utils libu2f-udev \
    libvulkan1 libxss1 libxtst6 libglib2.0-0 \
    --no-install-recommends && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copie de l'app compilée depuis le build
COPY --from=build /app/publish .
# Copie des navigateurs installés par Playwright
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Port exposé
EXPOSE 8080

# Commande de lancement
ENTRYPOINT ["dotnet", "RootBackend.dll"]