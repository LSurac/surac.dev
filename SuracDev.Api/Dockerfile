FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SuracDev.Api/SuracDev.Api.csproj SuracDev.Api/
RUN dotnet restore SuracDev.Api/SuracDev.Api.csproj

COPY SuracDev.Api/ SuracDev.Api/
RUN dotnet publish SuracDev.Api/SuracDev.Api.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

# The Data Protection key ring and the uploaded documents live on named volumes. Docker copies the
# ownership of these directories into a new volume on first mount, so they must belong to the
# non-root app user before USER switches.
RUN mkdir -p /keys /documents && chown $APP_UID:$APP_UID /keys /documents

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "SuracDev.Api.dll"]
