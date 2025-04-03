# Étape base (runtime pur)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Dépendances système nécessaires à Playwright
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    ca-certificates \
    fonts-liberation \
    libappindicator3-1 \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libgdk-pixbuf2.0-0 \
    libnspr4 \
    libnss3 \
    libx11-xcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    xdg-utils \
    libu2f-udev \
    libvulkan1 \
    libxss1 \
    libxtst6 \
    libglib2.0-0 \
    --no-install-recommends \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

# Étape de build avec le SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copie + restore
COPY ["RootBackend.csproj", "."]
RUN dotnet restore "./RootBackend.csproj"

# Copie tout le projet
COPY . .

# Build AVANT l’installation de Playwright
RUN dotnet build "./RootBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Install Playwright CLI et navigateurs (post-build obligatoire)
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install --with-deps

# Étape de publication
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RootBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Étape finale (runtime pur)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RootBackend.dll"]
