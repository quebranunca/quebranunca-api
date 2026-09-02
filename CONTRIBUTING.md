# Como contribuir

## Fluxo de trabalho

1. Atualize sua branch a partir de `main`.
2. Use uma branch curta e descritiva, como `feat/...`, `fix/...` ou `codex/...`.
3. Preserve a arquitetura `Api`, `Aplicacao`, `Dominio` e `Infraestrutura`.
4. Adicione ou ajuste testes compatíveis com o risco da mudança.
5. Abra um pull request pequeno, com contexto, validação e impacto de banco/deploy.

Mudanças entram por pull request com o status obrigatório `test-and-build` aprovado. Force push e exclusão de `main` não fazem parte do fluxo normal.

## Validação local

```bash
dotnet build PlataformaFutevolei.sln --configuration Release
dotnet test PlataformaFutevolei.sln --configuration Release
```

Testes de integração precisam de PostgreSQL e da variável `QNF_TEST_DATABASE_URL`.

## Banco e configuração

- Evolua o schema somente por migrations do EF Core.
- Confira se migrations manuais aparecem em `dotnet ef migrations list`.
- Não versione connection strings, JWT, senhas, tokens, códigos de acesso ou dados pessoais.
- Use `dotnet user-secrets` ou variáveis do ambiente.
- Atualize documentação e `Contextos/` quando uma decisão recorrente mudar.

## Pull request

Descreva o problema, a solução, os testes executados, as migrations e qualquer passo operacional. Para vulnerabilidades, use exclusivamente o processo privado de [SECURITY.md](SECURITY.md).
