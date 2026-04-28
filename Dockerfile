FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PlataformaFutevolei.Api/PlataformaFutevolei.Api.csproj", "PlataformaFutevolei.Api/"]
COPY ["PlataformaFutevolei.Aplicacao/PlataformaFutevolei.Aplicacao.csproj", "PlataformaFutevolei.Aplicacao/"]
COPY ["PlataformaFutevolei.Dominio/PlataformaFutevolei.Dominio.csproj", "PlataformaFutevolei.Dominio/"]
COPY ["PlataformaFutevolei.Infraestrutura/PlataformaFutevolei.Infraestrutura.csproj", "PlataformaFutevolei.Infraestrutura/"]

RUN dotnet restore "PlataformaFutevolei.Api/PlataformaFutevolei.Api.csproj"

COPY . .
WORKDIR /src/PlataformaFutevolei.Api
RUN dotnet publish "PlataformaFutevolei.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:${PGPORT}

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PlataformaFutevolei.Api.dll"]