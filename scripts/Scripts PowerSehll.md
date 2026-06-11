Scripts PowerSehll
----



----

dotnet build --configuration Release --no-restore

ASPNETCORE_ENVIRONMENT=Production \
ConnectionStrings__DefaultConnection='<definir-via-variavel-de-ambiente-ou-prompt-seguro>' \
Jwt__Chave='<definir-via-variavel-de-ambiente-ou-key-vault>' \
Frontend__Url='https://app.quebranunca.com.br' \
dotnet ef database update \
  --project PlataformaFutevolei.Infraestrutura \
  --startup-project PlataformaFutevolei.Api \
  --configuration Release \
  --no-build

---

export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection='<definir-via-variavel-de-ambiente-ou-prompt-seguro>'
export Jwt__Chave='<definir-via-variavel-de-ambiente-ou-key-vault>'
export Frontend__Url='https://app.quebranunca.com.br'

dotnet ef database update \
  --project PlataformaFutevolei.Infraestrutura \
  --startup-project PlataformaFutevolei.Api \
  --configuration Release
