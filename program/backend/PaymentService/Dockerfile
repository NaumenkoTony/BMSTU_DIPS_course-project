FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Contracts/*.csproj Contracts/
COPY PaymentService/*.csproj PaymentService/
RUN dotnet restore PaymentService/PaymentService.csproj

COPY Contracts/ Contracts/
COPY PaymentService/ PaymentService/

WORKDIR /src/PaymentService
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8060
ENTRYPOINT ["dotnet", "PaymentService.dll"]
