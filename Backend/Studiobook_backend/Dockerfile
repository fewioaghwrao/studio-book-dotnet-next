FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Backend/Studiobook_backend/Studiobook_backend.csproj \
     Backend/Studiobook_backend/

RUN dotnet restore \
    Backend/Studiobook_backend/Studiobook_backend.csproj

COPY Backend/Studiobook_backend/ \
     Backend/Studiobook_backend/

WORKDIR /src/Backend/Studiobook_backend

RUN dotnet publish \
    Studiobook_backend.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Studiobook_backend.dll"]