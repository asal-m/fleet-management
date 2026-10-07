FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY tools/ComposeBootstrap/ ./tools/ComposeBootstrap/
RUN dotnet publish tools/ComposeBootstrap/ComposeBootstrap.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir /run/fleet && chown $APP_UID /run/fleet
USER $APP_UID
ENTRYPOINT ["dotnet", "ComposeBootstrap.dll"]
