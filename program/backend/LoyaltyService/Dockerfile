FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Contracts/*.csproj Contracts/
COPY LoyaltyService/*.csproj LoyaltyService/
RUN dotnet restore LoyaltyService/LoyaltyService.csproj

COPY Contracts/ Contracts/
COPY LoyaltyService/ LoyaltyService/

WORKDIR /src/LoyaltyService
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8050
ENTRYPOINT ["dotnet", "LoyaltyService.dll"]
