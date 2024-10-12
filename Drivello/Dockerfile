FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./Drivello.csproj", "./"]
RUN dotnet restore "Drivello.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "Drivello.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Drivello.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set permissions
USER root
RUN chmod -R 755 /app \
    && find /app -type f -exec chmod 644 {} \; \
    && chmod 755 /app/Drivello.dll \
    && chown -R $APP_UID:$APP_UID /app

# Switch back to non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "Drivello.dll"]
