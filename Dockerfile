FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY src/backend/Copilot.Api/Copilot.Api.csproj src/backend/Copilot.Api/
RUN dotnet restore src/backend/Copilot.Api/Copilot.Api.csproj

COPY src/backend/Copilot.Api/ src/backend/Copilot.Api/
RUN dotnet publish src/backend/Copilot.Api/Copilot.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Copilot.Api.dll"]
