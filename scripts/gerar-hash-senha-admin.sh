#!/usr/bin/env bash
set -euo pipefail

PACOTE_BCRYPT_VERSAO="4.0.3"
TMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/qn-bcrypt.XXXXXX")"

cleanup() {
  rm -rf "$TMP_DIR"
}

trap cleanup EXIT

read -r -s -p "Senha temporaria do ADM: " SENHA
printf '\n' >&2

if [ "${#SENHA}" -lt 8 ]; then
  printf 'Senha precisa ter pelo menos 8 caracteres.\n' >&2
  exit 1
fi

cd "$TMP_DIR"

dotnet new console --force >/dev/null
dotnet add package BCrypt.Net-Next --version "$PACOTE_BCRYPT_VERSAO" >/dev/null

cat > Program.cs <<'CS'
var senha = Console.In.ReadToEnd();

if (string.IsNullOrWhiteSpace(senha))
{
    Console.Error.WriteLine("Senha nao informada.");
    Environment.Exit(1);
}

Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(senha));
CS

printf '%s' "$SENHA" | dotnet run --no-restore
unset SENHA
