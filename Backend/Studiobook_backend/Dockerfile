FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# レイヤー分割したプロジェクトの csproj を先にコピーして restore をキャッシュする
COPY Backend/Studiobook_backend.Domain/Studiobook_backend.Domain.csproj \
     Backend/Studiobook_backend.Domain/
COPY Backend/Studiobook_backend.Application/Studiobook_backend.Application.csproj \
     Backend/Studiobook_backend.Application/
COPY Backend/Studiobook_backend.Infrastructure/Studiobook_backend.Infrastructure.csproj \
     Backend/Studiobook_backend.Infrastructure/
COPY Backend/Studiobook_backend/Studiobook_backend.csproj \
     Backend/Studiobook_backend/

RUN dotnet restore \
    Backend/Studiobook_backend/Studiobook_backend.csproj

COPY Backend/Studiobook_backend.Domain/ \
     Backend/Studiobook_backend.Domain/
COPY Backend/Studiobook_backend.Application/ \
     Backend/Studiobook_backend.Application/
COPY Backend/Studiobook_backend.Infrastructure/ \
     Backend/Studiobook_backend.Infrastructure/
COPY Backend/Studiobook_backend/ \
     Backend/Studiobook_backend/

WORKDIR /src/Backend/Studiobook_backend

RUN dotnet publish \
    Studiobook_backend.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Studiobook_backend.dll"]
