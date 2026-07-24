#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_DIR="$ROOT_DIR/quebranunca-api"
API_PROJECT_DIR="$API_DIR/PlataformaFutevolei.Api"
WEB_DIR="$ROOT_DIR/quebranunca-web"
INFRA_DIR="$API_DIR/infra"
DOCKER_COMPOSE_FILE="$INFRA_DIR/docker-compose.yml"
RUNTIME_DIR="${TMPDIR:-/tmp}/plataforma-futevolei-local"

API_PID_FILE="$RUNTIME_DIR/api.pid"
WEB_PID_FILE="$RUNTIME_DIR/web.pid"
API_LOG_FILE="$RUNTIME_DIR/api.log"
WEB_LOG_FILE="$RUNTIME_DIR/web.log"

API_PORT="${API_PORT:-5000}"
WEB_PORT="${WEB_PORT:-5173}"
DB_PORT="${DB_PORT:-55432}"
DB_NAME="${DB_NAME:-plataforma_futevolei_dev}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-postgres}"
BUILD_BEFORE_RUN="${BUILD_BEFORE_RUN:-0}"
JWT_CHAVE="${JWT_CHAVE:-chave-desenvolvimento-local-script-quebranunca-2026}"
OPEN_BROWSER="${OPEN_BROWSER:-1}"

DEFAULT_CONNECTION_STRING="Host=localhost;Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};Ssl Mode=Disable"
API_DLL_PATH="$API_PROJECT_DIR/bin/Debug/net10.0/PlataformaFutevolei.Api.dll"

mkdir -p "$RUNTIME_DIR"

log() {
  printf '%s\n' "$*"
}

abrir_url_no_navegador() {
  local url="$1"

  if [[ "$OPEN_BROWSER" != "1" ]]; then
    return 0
  fi

  if command -v open >/dev/null 2>&1; then
    open "$url" >/dev/null 2>&1 || true
    return 0
  fi

  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" >/dev/null 2>&1 || true
  fi
}

executar_docker_compose() {
  (
    cd "$INFRA_DIR"
    POSTGRES_DB="$DB_NAME" \
    POSTGRES_USER="$DB_USER" \
    POSTGRES_PASSWORD="$DB_PASSWORD" \
    POSTGRES_PORT="$DB_PORT" \
    API_PORT="$API_PORT" \
    FRONTEND_PORT="$WEB_PORT" \
    docker compose -f "$DOCKER_COMPOSE_FILE" "$@"
  )
}

processo_ativo() {
  local pid="$1"
  kill -0 "$pid" >/dev/null 2>&1
}

ler_pid() {
  local arquivo="$1"
  if [[ -f "$arquivo" ]]; then
    cat "$arquivo"
  fi
}

encerrar_pid_arquivo() {
  local arquivo="$1"
  local nome="$2"
  local pid
  pid="$(ler_pid "$arquivo")"

  if [[ -z "${pid:-}" ]]; then
    rm -f "$arquivo"
    return 0
  fi

  if processo_ativo "$pid"; then
    log "Encerrando $nome (PID $pid)..."
    kill "$pid" >/dev/null 2>&1 || true

    for _ in {1..20}; do
      if ! processo_ativo "$pid"; then
        break
      fi
      sleep 0.5
    done

    if processo_ativo "$pid"; then
      log "$nome não respondeu ao encerramento gracioso. Finalizando processo..."
      kill -9 "$pid" >/dev/null 2>&1 || true
    fi
  fi

  rm -f "$arquivo"
}

porta_em_uso() {
  local porta="$1"
  lsof -tiTCP:"$porta" -sTCP:LISTEN 2>/dev/null | head -n 1
}

validar_porta_livre() {
  local porta="$1"
  local pid_esperado="$2"
  local nome="$3"
  local pid_ocupando

  pid_ocupando="$(porta_em_uso "$porta" || true)"
  if [[ -z "${pid_ocupando:-}" ]]; then
    return 0
  fi

  if [[ -n "${pid_esperado:-}" && "$pid_ocupando" == "$pid_esperado" ]]; then
    return 0
  fi

  log "A porta $porta já está em uso por outro processo (PID $pid_ocupando). Libere a porta antes de iniciar o $nome."
  exit 1
}

aguardar_http() {
  local url="$1"
  local descricao="$2"

  for _ in {1..60}; do
    if curl -fsS "$url" >/dev/null 2>&1; then
      log "$descricao disponível em $url"
      return 0
    fi
    sleep 1
  done

  log "Não foi possível confirmar $descricao em $url dentro do tempo esperado."
  return 1
}

alertar_banco() {
  if command -v pg_isready >/dev/null 2>&1; then
    if ! pg_isready -h localhost -p "$DB_PORT" >/dev/null 2>&1; then
      log "Aviso: PostgreSQL não respondeu em localhost:$DB_PORT. A API pode subir em Development, mas endpoints dependentes de banco não vão funcionar até o banco estar disponível."
    fi
  fi
}

garantir_docker_disponivel() {
  if ! command -v docker >/dev/null 2>&1; then
    log "Docker não está instalado ou não está no PATH."
    exit 1
  fi

  if ! docker info >/dev/null 2>&1; then
    log "Docker não está disponível. Inicie o Docker Desktop antes de rodar o script."
    exit 1
  fi
}

aguardar_postgres() {
  if ! command -v pg_isready >/dev/null 2>&1; then
    log "pg_isready não encontrado. Seguindo após subir o contêiner do PostgreSQL."
    return 0
  fi

  for _ in {1..60}; do
    if pg_isready -h localhost -p "$DB_PORT" >/dev/null 2>&1; then
      log "PostgreSQL disponível em localhost:$DB_PORT"
      return 0
    fi
    sleep 1
  done

  log "PostgreSQL não respondeu em localhost:$DB_PORT dentro do tempo esperado."
  return 1
}

iniciar_postgres_docker() {
  garantir_docker_disponivel
  log "Subindo PostgreSQL no Docker em localhost:${DB_PORT}"
  executar_docker_compose up -d postgres >/dev/null

  if ! aguardar_postgres; then
    log "Status do contêiner postgres:"
    executar_docker_compose ps postgres || true
    exit 1
  fi
}

parar_postgres_docker() {
  if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    log "Parando PostgreSQL no Docker..."
    executar_docker_compose stop postgres >/dev/null || true
  fi
}

iniciar_api() {
  local pid_existente
  pid_existente="$(ler_pid "$API_PID_FILE")"
  validar_porta_livre "$API_PORT" "$pid_existente" "backend"

  export ASPNETCORE_ENVIRONMENT=Development
  export PORT="$API_PORT"
  export ASPNETCORE_URLS="http://localhost:${API_PORT}"
  export Frontend__Url="http://localhost:${WEB_PORT};http://127.0.0.1:${WEB_PORT}"
  export EmailConvitesCadastro__UrlApp="http://localhost:${WEB_PORT}"
  export WhatsappConvitesCadastro__UrlApp="http://localhost:${WEB_PORT}"
  export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-$DEFAULT_CONNECTION_STRING}"
  export Jwt__Chave="${Jwt__Chave:-$JWT_CHAVE}"

  if [[ "$BUILD_BEFORE_RUN" == "1" || ! -f "$API_DLL_PATH" ]]; then
    log "Compilando backend..."
    (
      cd "$API_DIR"
      dotnet build PlataformaFutevolei.sln >/dev/null
    )
  fi

  log "Iniciando backend em http://localhost:${API_PORT}"
  cd "$API_PROJECT_DIR"
  nohup dotnet "$API_DLL_PATH" >"$API_LOG_FILE" 2>&1 &
  echo $! >"$API_PID_FILE"
  cd "$ROOT_DIR"

  if ! aguardar_http "http://localhost:${API_PORT}/health" "Backend"; then
    log "Logs do backend:"
    tail -n 40 "$API_LOG_FILE" || true
    exit 1
  fi
}

iniciar_frontend() {
  local pid_existente
  pid_existente="$(ler_pid "$WEB_PID_FILE")"
  validar_porta_livre "$WEB_PORT" "$pid_existente" "frontend"

  log "Iniciando frontend em http://localhost:${WEB_PORT}"
  cd "$WEB_DIR"
  nohup env VITE_API_URL="http://localhost:${API_PORT}" npm run dev -- --host 0.0.0.0 --port "$WEB_PORT" >"$WEB_LOG_FILE" 2>&1 &
  echo $! >"$WEB_PID_FILE"
  cd "$ROOT_DIR"

  if ! aguardar_http "http://localhost:${WEB_PORT}" "Frontend"; then
    log "Logs do frontend:"
    tail -n 40 "$WEB_LOG_FILE" || true
    exit 1
  fi
}

status() {
  local api_pid web_pid
  api_pid="$(ler_pid "$API_PID_FILE")"
  web_pid="$(ler_pid "$WEB_PID_FILE")"

  if [[ -n "${api_pid:-}" ]] && processo_ativo "$api_pid"; then
    log "Backend: ativo (PID $api_pid) em http://localhost:${API_PORT}"
  else
    log "Backend: parado"
  fi

  if [[ -n "${web_pid:-}" ]] && processo_ativo "$web_pid"; then
    log "Frontend: ativo (PID $web_pid) em http://localhost:${WEB_PORT}"
  else
    log "Frontend: parado"
  fi

  if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    local status_postgres
    status_postgres="$(executar_docker_compose ps --status running postgres 2>/dev/null | tail -n +2 || true)"
    if [[ -n "${status_postgres:-}" ]]; then
      log "PostgreSQL Docker: ativo em localhost:${DB_PORT}"
    else
      log "PostgreSQL Docker: parado"
    fi
  else
    log "PostgreSQL Docker: Docker indisponível"
  fi

  log "Logs:"
  log "  Backend: $API_LOG_FILE"
  log "  Frontend: $WEB_LOG_FILE"
}

start() {
  encerrar_pid_arquivo "$API_PID_FILE" "backend antigo"
  encerrar_pid_arquivo "$WEB_PID_FILE" "frontend antigo"
  iniciar_postgres_docker
  iniciar_api
  iniciar_frontend
  abrir_url_no_navegador "http://localhost:${WEB_PORT}"
  abrir_url_no_navegador "http://localhost:${API_PORT}/swagger"
  status
}

stop() {
  encerrar_pid_arquivo "$WEB_PID_FILE" "frontend"
  encerrar_pid_arquivo "$API_PID_FILE" "backend"
  parar_postgres_docker
  status
}

case "${1:-start}" in
  start)
    start
    ;;
  stop)
    stop
    ;;
  status)
    status
    ;;
  restart)
    stop
    start
    ;;
  *)
    log "Uso: $0 [start|stop|status|restart]"
    exit 1
    ;;
esac
