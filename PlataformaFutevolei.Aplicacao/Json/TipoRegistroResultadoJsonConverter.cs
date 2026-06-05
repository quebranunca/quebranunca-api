using System.Text.Json;
using System.Text.Json.Serialization;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Json;

public sealed class TipoRegistroResultadoJsonConverter : JsonConverter<TipoRegistroResultado>
{
    public override TipoRegistroResultado Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var valorNumerico))
        {
            return valorNumerico switch
            {
                (int)TipoRegistroResultado.PlacarDetalhado => TipoRegistroResultado.PlacarDetalhado,
                (int)TipoRegistroResultado.ApenasResultado => TipoRegistroResultado.ApenasResultado,
                _ => throw new JsonException("Tipo de registro de resultado inválido.")
            };
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Tipo de registro de resultado inválido.");
        }

        var valor = reader.GetString();
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new JsonException("Tipo de registro de resultado inválido.");
        }

        return valor.Trim() switch
        {
            nameof(TipoRegistroResultado.PlacarDetalhado) => TipoRegistroResultado.PlacarDetalhado,
            "PlacarCompleto" => TipoRegistroResultado.PlacarDetalhado,
            nameof(TipoRegistroResultado.ApenasResultado) => TipoRegistroResultado.ApenasResultado,
            _ => throw new JsonException("Tipo de registro de resultado inválido.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TipoRegistroResultado value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
