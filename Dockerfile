# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia csproj primeiro para cache
COPY src/UrlShortener/UrlShortener.csproj ./UrlShortener/
WORKDIR /src/UrlShortener
RUN dotnet restore

# Copia o restante do código
COPY src/UrlShortener/. ./
RUN dotnet publish -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
ENV DOTNET_URLS=http://+:80
ENTRYPOINT ["dotnet", "UrlShortener.dll"]
