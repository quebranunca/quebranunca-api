#!/bin/bash

# Script para gerar hash de senha para administrador usando BCrypt
# Uso: ./gerar-hash-senha-admin.sh <senha>

if [ $# -ne 1 ]; then
    echo "Uso: $0 <senha>"
    exit 1
fi

SENHA="$1"

# Criar diretório temporário
TEMP_DIR=$(mktemp -d)

# Criar arquivo .csproj
cat > "$TEMP_DIR/GerarHash.csproj" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
EOF

# Criar arquivo Program.cs
cat > "$TEMP_DIR/Program.cs" << 'EOF'
using System;
using BCrypt.Net;

class Program {
    static void Main(string[] args) {
        if (args.Length != 1) {
            Console.Error.WriteLine("Uso: dotnet run -- <senha>");
            return;
        }
        string hash = BCrypt.Net.BCrypt.HashPassword(args[0]);
        Console.WriteLine(hash);
    }
}
EOF

# Compilar e executar
cd "$TEMP_DIR"
dotnet run -- "$SENHA"

# Limpar
cd -
rm -rf "$TEMP_DIR"