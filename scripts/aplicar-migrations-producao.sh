#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_DIR="$ROOT_DIR/quebranunca-api"
PROJETO_INFRA="$API_DIR/PlataformaFutevolei.Infraestrutura/PlataformaFutevolei.Infraestrutura.csproj"
PROJETO_API="$API_DIR/PlataformaFutevolei.Api/PlataformaFutevolei.Api.csproj"
CONTEXTO="PlataformaFutevoleiDbContext"

if ! command -v dotnet >/dev/null 2>&1; then
  printf 'dotnet SDK não encontrado no PATH.\n' >&2
  exit 1
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  printf 'dotnet-ef não encontrado. Instale com: dotnet tool install --global dotnet-ef --version 10.0.8\n' >&2
  exit 1
fi

if [ -z "${ConnectionStrings__DefaultConnection:-}" ] && [ -z "${ConnectionStrings__Padrao:-}" ]; then
  read -r -s -p "Connection string de produção: " CONNECTION_STRING_PRODUCAO
  printf '\n' >&2

  if [ -z "$CONNECTION_STRING_PRODUCAO" ]; then
    printf 'Connection string não informada.\n' >&2
    exit 1
  fi

  export ConnectionStrings__DefaultConnection="$CONNECTION_STRING_PRODUCAO"
  unset CONNECTION_STRING_PRODUCAO
fi

export ASPNETCORE_ENVIRONMENT=Production
export DOTNET_ENVIRONMENT=Production
export Jwt__Chave="${Jwt__Chave:-chave-temporaria-apenas-para-design-time-do-ef-core-com-tamanho-suficiente}"
export Jwt__Emissor="${Jwt__Emissor:-PlataformaFutevolei.Api}"
export Jwt__Audiencia="${Jwt__Audiencia:-PlataformaFutevolei.Web}"
export Frontend__Url="${Frontend__Url:-https://app.quebranunca.com.br}"

printf 'Listando migrations com o banco configurado...\n' >&2
dotnet ef migrations list \
  --project "$PROJETO_INFRA" \
  --startup-project "$PROJETO_API" \
  --context "$CONTEXTO"

printf '\nEste comando aplicará migrations pendentes no banco configurado.\n' >&2
read -r -p "Digite APLICAR para continuar: " CONFIRMACAO

if [ "$CONFIRMACAO" != "APLICAR" ]; then
  printf 'Operação cancelada.\n' >&2
  exit 1
fi

dotnet ef database update \
  --project "$PROJETO_INFRA" \
  --startup-project "$PROJETO_API" \
  --context "$CONTEXTO"

printf 'Migrations aplicadas com sucesso.\n' >&2
