# Build context: src/backend (see docker-compose.yml at repo root)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props .editorconfig ./
COPY src/SalesDeliveryBI.Domain/SalesDeliveryBI.Domain.csproj SalesDeliveryBI.Domain/
COPY src/SalesDeliveryBI.Application/SalesDeliveryBI.Application.csproj SalesDeliveryBI.Application/
COPY src/SalesDeliveryBI.Infrastructure/SalesDeliveryBI.Infrastructure.csproj SalesDeliveryBI.Infrastructure/
COPY src/SalesDeliveryBI.Api/SalesDeliveryBI.Api.csproj SalesDeliveryBI.Api/
RUN dotnet restore SalesDeliveryBI.Api/SalesDeliveryBI.Api.csproj

COPY src/ .
RUN dotnet publish SalesDeliveryBI.Api/SalesDeliveryBI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed by docker-compose's healthcheck (the base image ships neither curl nor wget).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SalesDeliveryBI.Api.dll"]
