FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env

COPY ./HackerNews /app
WORKDIR /app

RUN ["dotnet", "build"]

ENTRYPOINT ["dotnet", "run"]