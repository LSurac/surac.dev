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

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "SuracDev.Api.dll"]
