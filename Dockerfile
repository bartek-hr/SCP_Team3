# The test stage is deliberately on the dependency path to the final image.
# A normal `docker build` therefore cannot publish a runtime image if tests fail.
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY CargoHUB.sln CargoHUB.csproj ./
COPY CargoHUB.Tests/CargoHUB.Tests.csproj CargoHUB.Tests/
RUN dotnet restore CargoHUB.sln

COPY . ./
RUN dotnet build CargoHUB.sln --configuration Release --no-restore

FROM build AS test
RUN dotnet test CargoHUB.sln --configuration Release --no-build --no-restore \
    --logger "console;verbosity=normal"

FROM test AS publish
RUN dotnet publish CargoHUB.csproj --configuration Release --no-build --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --uid 10001 --create-home --home-dir /app --shell /usr/sbin/nologin cargohub

WORKDIR /app
COPY --from=publish --chown=cargohub:cargohub /app/publish ./
RUN mkdir /app/data && chown cargohub:cargohub /app/data

USER cargohub
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataDirectory=/app/data
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=6 \
    CMD curl --fail --silent http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "CargoHUB.dll"]
