# ---- Base runtime image ----
    FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
    WORKDIR /app
    EXPOSE 5000
    
    # ---- Final stage ----
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final
    WORKDIR /app
    COPY ./out .
    
    # environment vars
    ENV ASPNETCORE_URLS=http://+:5000
    
    # run migrations before app start
    CMD sh -c "dotnet ef database update --no-build || true && dotnet ChatApi.dll"
    