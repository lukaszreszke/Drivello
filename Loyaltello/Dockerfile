FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Loyaltello/Loyaltello.csproj", "Loyaltello/"]
COPY ["Shared.Messages/Shared.Messages.csproj", "Shared.Messages/"]

WORKDIR /src/Loyaltello
RUN dotnet restore "Loyaltello.csproj"

COPY ["Loyaltello/", "/src/Loyaltello/"]
COPY ["Shared.Messages/", "/src/Shared.Messages/"]

RUN dotnet build "Loyaltello.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Loyaltello.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN chmod -R 755 /app \
    && find /app -type f -exec chmod 644 {} \; \
    && chmod 755 /app/Loyaltello.dll \
    && chown -R $APP_UID:$APP_UID /app

USER $APP_UID

ENTRYPOINT ["dotnet", "Loyaltello.dll"]