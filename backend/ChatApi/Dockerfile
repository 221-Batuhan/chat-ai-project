FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY ./out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet","ChatApi.dll"]
