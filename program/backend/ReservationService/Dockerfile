FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Contracts/*.csproj Contracts/
COPY ReservationService/*.csproj ReservationService/
RUN dotnet restore ReservationService/ReservationService.csproj

COPY Contracts/ Contracts/
COPY ReservationService/ ReservationService/

WORKDIR /src/ReservationService
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8070
ENTRYPOINT ["dotnet", "ReservationService.dll"]
