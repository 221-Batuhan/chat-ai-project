# 1️⃣ Base image - runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# 2️⃣ Build image - derleme ortamı
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# 3️⃣ Final stage - runtime container
FROM base AS final
WORKDIR /app
COPY --from=build /app/out .

# 4️⃣ Uygulama dinamik PORT değerini Railway'den alsın
ENV ASPNETCORE_URLS=http://+:${PORT}

# 5️⃣ Veritabanı migration’larını çalıştır, sonra API’yi başlat
CMD sh -c "dotnet ef database update --no-build || true && dotnet ChatApi.dll"
