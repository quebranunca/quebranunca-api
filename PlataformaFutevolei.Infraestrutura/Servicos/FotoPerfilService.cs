using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class FotoPerfilService(
    IOptions<CloudinaryConfiguracao> configuracaoAccessor,
    ILogger<FotoPerfilService> logger
) : IFotoPerfilService
{
    private const long TamanhoMaximoBytes = 2 * 1024 * 1024;
    private const string PastaPerfis = "quebranunca/perfis";
    private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public async Task<FotoPerfilUploadDto> EnviarAsync(
        ArquivoFotoPerfilDto arquivo,
        CancellationToken cancellationToken = default)
    {
        ValidarArquivo(arquivo);

        var cloudinary = CriarClienteConfigurado();
        var upload = new ImageUploadParams
        {
            File = new FileDescription(arquivo.NomeArquivo, arquivo.Conteudo),
            Folder = PastaPerfis,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = true,
            Transformation = CriarTransformacaoAvatar()
        };

        ImageUploadResult resultado;
        try
        {
            resultado = await cloudinary.UploadAsync(upload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar foto de perfil para o Cloudinary.");
            throw new RegraNegocioException("Não foi possível enviar a foto de perfil. Tente novamente.");
        }

        if (resultado.Error is not null || string.IsNullOrWhiteSpace(resultado.PublicId))
        {
            logger.LogWarning("Cloudinary retornou erro ao enviar foto de perfil: {Erro}", resultado.Error?.Message);
            throw new RegraNegocioException("Não foi possível enviar a foto de perfil. Tente novamente.");
        }

        var url = cloudinary.Api.UrlImgUp
            .Secure(true)
            .Transform(CriarTransformacaoAvatar())
            .BuildUrl(resultado.PublicId);

        return new FotoPerfilUploadDto(url, resultado.PublicId);
    }

    public async Task RemoverAsync(string publicId, CancellationToken cancellationToken = default)
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
                ResourceType = ResourceType.Image
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao remover foto de perfil anterior do Cloudinary.");
        }
    }

    private static void ValidarArquivo(ArquivoFotoPerfilDto arquivo)
    {
        if (arquivo.Conteudo is null || arquivo.TamanhoBytes <= 0)
        {
            throw new RegraNegocioException("Arquivo da foto de perfil é obrigatório.");
        }

        if (arquivo.TamanhoBytes > TamanhoMaximoBytes)
        {
            throw new RegraNegocioException("A foto de perfil deve ter no máximo 2MB.");
        }

        var extensao = Path.GetExtension(arquivo.NomeArquivo);
        if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
        {
            throw new RegraNegocioException("Formato de foto inválido. Envie uma imagem JPG, PNG ou WEBP.");
        }
    }

    private Cloudinary CriarClienteConfigurado()
    {
        var configuracao = configuracaoAccessor.Value;
        if (!configuracao.EstaConfigurado())
        {
            logger.LogWarning(
                "Configuração Cloudinary incompleta. CloudName configurado: {CloudNameConfigurado}; ApiKey configurado: {ApiKeyConfigurado}; ApiSecret configurado: {ApiSecretConfigurado}",
                !string.IsNullOrWhiteSpace(configuracao.CloudName),
                !string.IsNullOrWhiteSpace(configuracao.ApiKey),
                !string.IsNullOrWhiteSpace(configuracao.ApiSecret));

            throw new RegraNegocioException("Cloudinary não configurado para envio de foto de perfil.");
        }

        var account = new Account(
            configuracao.CloudName,
            configuracao.ApiKey,
            configuracao.ApiSecret);

        return new Cloudinary(account);
    }

    private static Transformation CriarTransformacaoAvatar()
    {
        return new Transformation()
            .Width(400)
            .Height(400)
            .Crop("fill")
            .Gravity("face")
            .Quality("auto")
            .FetchFormat("auto");
    }
}
