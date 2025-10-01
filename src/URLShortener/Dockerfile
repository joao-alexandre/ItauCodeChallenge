FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["URLShortener.csproj", "./"]
RUN dotnet restore "./URLShortener.csproj"
COPY . .
RUN dotnet build "URLShortener.csproj" -c Release -o /app/build
RUN dotnet publish "URLShortener.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

ENTRYPOINT ["/wait-for-it.sh", "db:5432", "--", "dotnet", "URLShortener.dll"]
