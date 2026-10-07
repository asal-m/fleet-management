FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY tools/DevelopmentIdentity/ .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet","DevelopmentIdentity.dll"]
