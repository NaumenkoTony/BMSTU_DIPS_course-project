FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Contracts/*.csproj Contracts/
COPY IdentityService/*.csproj IdentityService/
RUN dotnet restore IdentityService/IdentityService.csproj

COPY Contracts/ Contracts/
COPY IdentityService/ IdentityService/

WORKDIR /src/IdentityService
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8000
ENTRYPOINT ["dotnet", "IdentityService.dll"]
