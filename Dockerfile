# Build and Publish
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY --link HackerNews/*.csproj .
RUN dotnet restore -a $TARGETARCH

COPY --link HackerNews/. .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

# Run
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --link --from=build /app .
ENTRYPOINT ["dotnet", "HackerNews.dll"]
