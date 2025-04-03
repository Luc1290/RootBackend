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

# Étape build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["RootBackend.csproj", "."]
RUN dotnet restore "./RootBackend.csproj"
COPY . .

# Build du projet
RUN dotnet build "./RootBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Install Playwright CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global Microsoft.Playwright.CLI

# ✅ Playwright exige que les DLL soient construites AVANT install
# => on publie d’abord dans un dossier temporaire
RUN dotnet publish "./RootBackend.csproj" -c $BUILD_CONFIGURATION -o /tmp/published /p:UseAppHost=false

# On installe Playwright dans les binaires publiés
WORKDIR /tmp/published
RUN /root/.dotnet/tools/playwright install --with-deps

# Étape finale
FROM base AS final
WORKDIR /app
COPY --from=build /tmp/published .
ENTRYPOINT ["dotnet", "RootBackend.dll"]
