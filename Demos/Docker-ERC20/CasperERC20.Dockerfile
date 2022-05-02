#FROM bitnami/dotnet-sdk:6.0.201
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY ../../casper-net-sdk-web /app/casper-net-sdk-web
COPY ../../casper-net-sdk-clients /app/casper-net-sdk-clients

WORKDIR "/app/casper-net-sdk-clients/Demos/CasperERC20"
RUN dotnet restore "CasperERC20.csproj"
RUN dotnet build "CasperERC20.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CasperERC20.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/casper-net-sdk-clients/Demos/CasperERC20/erc20_token.wasm .

ENTRYPOINT ["dotnet", "CasperERC20.dll"]

