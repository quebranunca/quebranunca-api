using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class MidiaPartidaService(
    IOptions<CloudinaryConfiguracao> configuracaoAccessor,
    ILogger<MidiaPartidaService> logger
) : IMidiaPartidaService
{
    private const long TamanhoMaximoImagemBytes = 20 * 1024 * 1024;
    private const long TamanhoMaximoVideoBytes = 100 * 1024 * 1024;
    private const string PastaPartidas = "quebranunca/partidas";

    private static readonly HashSet<string> ExtensoesImagemPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> ExtensoesVideoPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mov",
        ".webm"
    };

    private static readonly HashSet<string> ContentTypesImagemPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> ContentTypesVideoPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime",
        "video/webm"
    };

    public async Task<MidiaPartidaUploadDto> EnviarAsync(
        ArquivoMidiaPartidaDto arquivo,
        CancellationToken cancellationToken = default)
    {
        var tipo = ValidarArquivo(arquivo);
        var cloudinary = CriarClienteConfigurado();

        try
        {
            return tipo == MidiaPartidaTipo.Imagem
                ? await EnviarImagemAsync(cloudinary, arquivo, cancellationToken)
                : await EnviarVideoAsync(cloudinary, arquivo, cancellationToken);
        }
        catch (RegraNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar mídia da partida para o Cloudinary.");
            throw new RegraNegocioException("Não foi possível enviar a mídia da partida. Tente novamente.");
        }
    }

    public async Task RemoverAsync(
        string publicId,
        MidiaPartidaTipo? tipo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var cloudinary = CriarClienteConfigurado();
        try
        {
            await cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = tipo == MidiaPartidaTipo.Video ? ResourceType.Video : ResourceType.Image
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover mídia anterior da partida no Cloudinary.");
        }
    }

    private static async Task<MidiaPartidaUploadDto> EnviarImagemAsync(
        Cloudinary cloudinary,
        ArquivoMidiaPartidaDto arquivo,
        CancellationToken cancellationToken)
    {
        var upload = new ImageUploadParams
        {
            File = new FileDescription(arquivo.NomeArquivo, arquivo.Conteudo),
            Folder = PastaPartidas,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = true,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var resultado = await cloudinary.UploadAsync(upload, cancellationToken);
        ValidarResultadoUpload(resultado.Error?.Message, resultado.PublicId);

        return new MidiaPartidaUploadDto(
            resultado.SecureUrl?.ToString() ?? cloudinary.Api.UrlImgUp.Secure(true).BuildUrl(resultado.PublicId),
            resultado.PublicId,
            MidiaPartidaTipo.Imagem);
    }

    private static async Task<MidiaPartidaUploadDto> EnviarVideoAsync(
        Cloudinary cloudinary,
        ArquivoMidiaPartidaDto arquivo,
        CancellationToken cancellationToken)
    {
        var upload = new VideoUploadParams
        {
            File = new FileDescription(arquivo.NomeArquivo, arquivo.Conteudo),
            Folder = PastaPartidas,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = true
        };

        var resultado = await cloudinary.UploadAsync(upload, cancellationToken);
        ValidarResultadoUpload(resultado.Error?.Message, resultado.PublicId);

        return new MidiaPartidaUploadDto(
            resultado.SecureUrl?.ToString() ?? cloudinary.Api.UrlVideoUp.Secure(true).BuildUrl(resultado.PublicId),
            resultado.PublicId,
            MidiaPartidaTipo.Video);
    }

    private static void ValidarResultadoUpload(string? erro, string? publicId)
    {
        if (!string.IsNullOrWhiteSpace(erro) || string.IsNullOrWhiteSpace(publicId))
        {
            throw new RegraNegocioException("Não foi possível enviar a mídia da partida. Tente novamente.");
        }
    }

    private static MidiaPartidaTipo ValidarArquivo(ArquivoMidiaPartidaDto arquivo)
    {
        if (arquivo.Conteudo is null || arquivo.TamanhoBytes <= 0)
        {
            throw new RegraNegocioException("Arquivo da mídia da partida é obrigatório.");
        }

        var extensao = Path.GetExtension(arquivo.NomeArquivo);
        if (string.IsNullOrWhiteSpace(extensao))
        {
            throw new RegraNegocioException("Formato de mídia inválido.");
        }

        if (EhImagemPermitida(extensao, arquivo.ContentType))
        {
            if (arquivo.TamanhoBytes > TamanhoMaximoImagemBytes)
            {
                throw new RegraNegocioException("Imagens devem ter no máximo 20MB.");
            }

            return MidiaPartidaTipo.Imagem;
        }

        if (EhVideoPermitido(extensao, arquivo.ContentType))
        {
            if (arquivo.TamanhoBytes > TamanhoMaximoVideoBytes)
            {
                throw new RegraNegocioException("Vídeos devem ter no máximo 100MB.");
            }

            return MidiaPartidaTipo.Video;
        }

        throw new RegraNegocioException("Formato de mídia inválido. Envie JPG, PNG, WEBP, MP4, MOV ou WEBM.");
    }

    private static bool EhImagemPermitida(string extensao, string? contentType)
        => ExtensoesImagemPermitidas.Contains(extensao) ||
           (!string.IsNullOrWhiteSpace(contentType) && ContentTypesImagemPermitidos.Contains(contentType));

    private static bool EhVideoPermitido(string extensao, string? contentType)
        => ExtensoesVideoPermitidas.Contains(extensao) ||
           (!string.IsNullOrWhiteSpace(contentType) && ContentTypesVideoPermitidos.Contains(contentType));

    private Cloudinary CriarClienteConfigurado()
    {
        var configuracao = configuracaoAccessor.Value;
        if (!configuracao.EstaConfigurado())
        {
            logger.LogWarning(
                "Configuração Cloudinary incompleta para mídia de partida. CloudName configurado: {CloudNameConfigurado}; ApiKey configurado: {ApiKeyConfigurado}; ApiSecret configurado: {ApiSecretConfigurado}",
                !string.IsNullOrWhiteSpace(configuracao.CloudName),
                !string.IsNullOrWhiteSpace(configuracao.ApiKey),
                !string.IsNullOrWhiteSpace(configuracao.ApiSecret));

            throw new RegraNegocioException("Cloudinary não configurado para envio de mídia da partida.");
        }

        var account = new Account(
            configuracao.CloudName,
            configuracao.ApiKey,
            configuracao.ApiSecret);

        return new Cloudinary(account);
    }
}
