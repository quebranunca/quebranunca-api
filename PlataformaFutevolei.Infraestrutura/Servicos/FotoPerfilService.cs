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
    private const string PastaGrupos = "quebranunca/grupos";
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
        => await EnviarAsync(
            arquivo,
            PastaPerfis,
            "foto de perfil",
            "Arquivo da foto de perfil é obrigatório.",
            "A foto de perfil deve ter no máximo 2MB.",
            "Não foi possível enviar a foto de perfil. Tente novamente.",
            cancellationToken);

    public async Task<FotoPerfilUploadDto> EnviarGrupoAsync(
        ArquivoFotoPerfilDto arquivo,
        CancellationToken cancellationToken = default)
        => await EnviarAsync(
            arquivo,
            PastaGrupos,
            "foto do grupo",
            "Arquivo da foto do grupo é obrigatório.",
            "A foto do grupo deve ter no máximo 2MB.",
            "Não foi possível enviar a foto do grupo. Tente novamente.",
            cancellationToken);

    private async Task<FotoPerfilUploadDto> EnviarAsync(
        ArquivoFotoPerfilDto arquivo,
        string pasta,
        string descricaoLog,
        string mensagemObrigatoria,
        string mensagemTamanho,
        string mensagemFalha,
        CancellationToken cancellationToken)
    {
        ValidarArquivo(arquivo, mensagemObrigatoria, mensagemTamanho);

        var cloudinary = CriarClienteConfigurado();
        var upload = new ImageUploadParams
        {
            File = new FileDescription(arquivo.NomeArquivo, arquivo.Conteudo),
            Folder = pasta,
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
            logger.LogWarning(ex, "Falha ao enviar {DescricaoLog} para o Cloudinary.", descricaoLog);
            throw new RegraNegocioException(mensagemFalha);
        }

        if (resultado.Error is not null || string.IsNullOrWhiteSpace(resultado.PublicId))
        {
            logger.LogWarning("Cloudinary retornou erro ao enviar {DescricaoLog}: {Erro}", descricaoLog, resultado.Error?.Message);
            throw new RegraNegocioException(mensagemFalha);
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

    private static void ValidarArquivo(ArquivoFotoPerfilDto arquivo, string mensagemObrigatoria, string mensagemTamanho)
    {
        if (arquivo.Conteudo is null || arquivo.TamanhoBytes <= 0)
        {
            throw new RegraNegocioException(mensagemObrigatoria);
        }

        if (arquivo.TamanhoBytes > TamanhoMaximoBytes)
        {
            throw new RegraNegocioException(mensagemTamanho);
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
