FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ParagLog.sln .
COPY src/ParagLog.Core/ParagLog.Core.csproj src/ParagLog.Core/
COPY src/ParagLog.Infrastructure/ParagLog.Infrastructure.csproj src/ParagLog.Infrastructure/
COPY src/ParagLog.Api/ParagLog.Api.csproj src/ParagLog.Api/
RUN dotnet restore src/ParagLog.Api/ParagLog.Api.csproj

COPY src/ src/
RUN dotnet publish src/ParagLog.Api/ParagLog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ParagLog.Api.dll"]
