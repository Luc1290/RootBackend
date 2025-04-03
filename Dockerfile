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

# STAGE 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Copie de l'app compilée depuis le build
COPY --from=build /app/publish .
# Copie des navigateurs installés par Playwright
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Port exposé
EXPOSE 8080

# Commande de lancement
ENTRYPOINT ["dotnet", "RootBackend.dll"]