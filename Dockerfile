# Regneræven as a website (Azure Container Apps). Builds src/Regneraeven.Server – the Windows program is not included.
#   docker build -t regneraeven .
#   docker run -p 8080:8080 -e ConnectionStrings__Regneraeven="Server=...;Database=...;..." regneraeven

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore first (cached as long as the project files don't change).
COPY src/Regneraeven.Core/Regneraeven.Core.csproj src/Regneraeven.Core/
COPY src/Regneraeven.Web/Regneraeven.Web.csproj src/Regneraeven.Web/
COPY src/Regneraeven.Server/Regneraeven.Server.csproj src/Regneraeven.Server/
RUN dotnet restore src/Regneraeven.Server/Regneraeven.Server.csproj
COPY src/Regneraeven.Core/ src/Regneraeven.Core/
COPY src/Regneraeven.Web/ src/Regneraeven.Web/
COPY src/Regneraeven.Server/ src/Regneraeven.Server/
RUN dotnet publish src/Regneraeven.Server/Regneraeven.Server.csproj -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Danish dates and the "today" for daily tasks follow Danish time.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Copenhagen
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Regneraeven.Server.dll"]
