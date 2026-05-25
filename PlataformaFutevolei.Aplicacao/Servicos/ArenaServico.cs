using System.Globalization;
using System.Text;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class ArenaServico(
    IArenaRepositorio arenaRepositorio,
    IArenaResponsavelRepositorio arenaResponsavelRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IArenaServico
{
    public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
        ArenaFiltroPublicoRequest filtro,
        CancellationToken cancellationToken = default)
    {
        var filtroNormalizado = new ArenaFiltroPublicoRequest(
            Normalizar(filtro.Cidade),
            Normalizar(filtro.Estado),
            filtro.TipoArena,
            Normalizar(filtro.TermoBusca));

        if (filtroNormalizado.TipoArena.HasValue)
        {
            ValidarTipo(filtroNormalizado.TipoArena.Value);
        }

        return arenaRepositorio.ListarPublicasAsync(filtroNormalizado, cancellationToken);
    }

    public async Task<ArenaDetalhePublicoResponse> ObterPublicaPorSlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var slugNormalizado = Normalizar(slug);
        if (slugNormalizado is null)
        {
            throw new RegraNegocioException("Slug da arena é obrigatório.");
        }

        return await arenaRepositorio.ObterPublicaPorSlugAsync(slugNormalizado, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
    }

    public async Task<ArenaResumoPublicoResponse> ObterResumoPublicoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new RegraNegocioException("Arena é obrigatória.");
        }

        return await arenaRepositorio.ObterResumoPublicoAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
    }

    public async Task<IReadOnlyList<ArenaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var arenas = await arenaRepositorio.ListarAsync(cancellationToken);
        return arenas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<ArenaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var arena = await arenaRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        return arena.ParaDto();
    }

    public async Task<ArenaDto> CriarAsync(CriarArenaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var nome = ValidarNome(dto.Nome);
        ValidarTipo(dto.TipoArena);
        ValidarQuantidadeEspacos(dto.QuantidadeEspacos);
        ValidarCoordenadas(dto.Latitude, dto.Longitude);
        await ValidarNomeUnicoAsync(nome, null, cancellationToken);

        var slug = await ObterSlugUnicoAsync(nome, null, cancellationToken);
        var arena = new Arena
        {
            Nome = nome,
            Slug = slug,
            Descricao = Normalizar(dto.Descricao),
            TipoArena = dto.TipoArena,
            QuantidadeEspacos = dto.QuantidadeEspacos,
            Endereco = Normalizar(dto.Endereco),
            EnderecoResumo = Normalizar(dto.EnderecoResumo),
            Cidade = Normalizar(dto.Cidade),
            Estado = Normalizar(dto.Estado),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Whatsapp = Normalizar(dto.Whatsapp),
            Instagram = Normalizar(dto.Instagram),
            Site = Normalizar(dto.Site),
            LogoUrl = Normalizar(dto.LogoUrl),
            LogoPublicId = Normalizar(dto.LogoPublicId),
            CapaUrl = Normalizar(dto.CapaUrl),
            CapaPublicId = Normalizar(dto.CapaPublicId),
            Publica = dto.Publica ?? true,
            Ativa = dto.Ativa ?? true
        };

        var responsavel = new ArenaResponsavel
        {
            ArenaId = arena.Id,
            UsuarioId = usuario.Id,
            Papel = PapelArenaResponsavel.ArenaAdmin,
            Ativo = true,
            Usuario = usuario
        };
        arena.Responsaveis.Add(responsavel);

        await arenaRepositorio.AdicionarAsync(arena, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return arena.ParaDto();
    }

    public async Task<ArenaDto> AtualizarAsync(Guid id, AtualizarArenaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, id, cancellationToken);

        var nome = ValidarNome(dto.Nome);
        ValidarTipo(dto.TipoArena);
        ValidarQuantidadeEspacos(dto.QuantidadeEspacos);
        ValidarCoordenadas(dto.Latitude, dto.Longitude);
        await ValidarNomeUnicoAsync(nome, arena.Id, cancellationToken);

        arena.Nome = nome;
        arena.Slug = await ObterSlugUnicoAsync(nome, arena.Id, cancellationToken);
        arena.Descricao = Normalizar(dto.Descricao);
        arena.TipoArena = dto.TipoArena;
        arena.QuantidadeEspacos = dto.QuantidadeEspacos;
        arena.Endereco = Normalizar(dto.Endereco);
        arena.EnderecoResumo = Normalizar(dto.EnderecoResumo);
        arena.Cidade = Normalizar(dto.Cidade);
        arena.Estado = Normalizar(dto.Estado);
        arena.Latitude = dto.Latitude;
        arena.Longitude = dto.Longitude;
        arena.Whatsapp = Normalizar(dto.Whatsapp);
        arena.Instagram = Normalizar(dto.Instagram);
        arena.Site = Normalizar(dto.Site);
        arena.LogoUrl = Normalizar(dto.LogoUrl);
        arena.LogoPublicId = Normalizar(dto.LogoPublicId);
        arena.CapaUrl = Normalizar(dto.CapaUrl);
        arena.CapaPublicId = Normalizar(dto.CapaPublicId);
        arena.Publica = dto.Publica;
        arena.Ativa = dto.Ativa;
        arena.AtualizarDataModificacao();

        arenaRepositorio.Atualizar(arena);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return arena.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, id, cancellationToken);

        arenaRepositorio.Remover(arena);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task GarantirGestaoPermitidaAsync(Usuario usuario, Guid arenaId, CancellationToken cancellationToken)
    {
        if (usuario.Perfil == PerfilUsuario.Administrador)
        {
            return;
        }

        if (!await arenaResponsavelRepositorio.UsuarioPodeGerenciarAsync(arenaId, usuario.Id, cancellationToken))
        {
            throw new RegraNegocioException("O usuário só pode alterar arenas em que é responsável ativo.");
        }
    }

    private async Task ValidarNomeUnicoAsync(string nome, Guid? idIgnorado, CancellationToken cancellationToken)
    {
        var existente = await arenaRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null && existente.Id != idIgnorado)
        {
            throw new RegraNegocioException("Já existe uma arena cadastrada com este nome.");
        }
    }

    private async Task<string> ObterSlugUnicoAsync(string nome, Guid? idIgnorado, CancellationToken cancellationToken)
    {
        var slug = GerarSlug(nome);
        if (!await arenaRepositorio.ExisteSlugAsync(slug, idIgnorado, cancellationToken))
        {
            return slug;
        }

        throw new RegraNegocioException("Já existe uma arena cadastrada com este identificador.");
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome da arena é obrigatório.");
        }

        return nome.Trim();
    }

    private static void ValidarTipo(TipoArena tipoArena)
    {
        if (!Enum.IsDefined(tipoArena))
        {
            throw new RegraNegocioException("Tipo de arena inválido.");
        }
    }

    private static void ValidarQuantidadeEspacos(int quantidadeEspacos)
    {
        if (quantidadeEspacos <= 0)
        {
            throw new RegraNegocioException("Quantidade de espaços deve ser maior que zero.");
        }
    }

    private static void ValidarCoordenadas(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue ||
            latitude is < -90 or > 90 ||
            longitude is < -180 or > 180)
        {
            throw new RegraNegocioException("Latitude e longitude da arena devem ser informadas juntas e válidas.");
        }
    }

    private static string GerarSlug(string nome)
    {
        var normalizado = nome.Normalize(NormalizationForm.FormD);
        var caracteres = normalizado
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        var semAcentos = new string(caracteres).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var slug = new StringBuilder();
        var separadorPendente = false;

        foreach (var caractere in semAcentos)
        {
            if (char.IsLetterOrDigit(caractere))
            {
                if (separadorPendente && slug.Length > 0)
                {
                    slug.Append('-');
                }

                slug.Append(caractere);
                separadorPendente = false;
            }
            else
            {
                separadorPendente = true;
            }
        }

        return slug.Length == 0 ? "arena" : slug.ToString();
    }

    private static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
