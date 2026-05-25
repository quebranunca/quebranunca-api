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

    public async Task<ArenaAdminDetalheResponse> CriarAdminAsync(
        CriarArenaRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var nome = ValidarNome(request.Nome);
        ValidarTipo(request.TipoArena);
        ValidarQuantidadeEspacos(request.QuantidadeEspacos);
        ValidarCoordenadas(request.Latitude, request.Longitude);
        await ValidarNomeUnicoAsync(nome, null, cancellationToken);

        var arena = new Arena
        {
            Nome = nome,
            Slug = await ObterSlugUnicoAsync(nome, null, cancellationToken),
            Descricao = Normalizar(request.Descricao),
            TipoArena = request.TipoArena,
            QuantidadeEspacos = request.QuantidadeEspacos,
            Endereco = Normalizar(request.Endereco),
            EnderecoResumo = Normalizar(request.EnderecoResumo),
            Cidade = Normalizar(request.Cidade),
            Estado = Normalizar(request.Estado),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Whatsapp = Normalizar(request.Whatsapp),
            Instagram = Normalizar(request.Instagram),
            Site = Normalizar(request.Site),
            Publica = request.Publica,
            Ativa = true,
            PossuiIluminacao = request.PossuiIluminacao,
            PossuiEstacionamento = request.PossuiEstacionamento,
            PossuiVestiario = request.PossuiVestiario,
            PossuiDucha = request.PossuiDucha,
            PossuiBarRestaurante = request.PossuiBarRestaurante,
            PossuiLoja = request.PossuiLoja,
            PossuiCobertura = request.PossuiCobertura
        };

        arena.Responsaveis.Add(new ArenaResponsavel
        {
            ArenaId = arena.Id,
            UsuarioId = usuario.Id,
            Papel = PapelArenaResponsavel.ArenaAdmin,
            Ativo = true,
            Usuario = usuario
        });

        await arenaRepositorio.AdicionarAsync(arena, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return ParaDetalheAdmin(arena);
    }

    public async Task<ArenaAdminDetalheResponse> AtualizarAdminAsync(
        Guid arenaId,
        AtualizarArenaRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arenaId, cancellationToken);

        var nome = ValidarNome(request.Nome);
        ValidarTipo(request.TipoArena);
        ValidarQuantidadeEspacos(request.QuantidadeEspacos);
        ValidarCoordenadas(request.Latitude, request.Longitude);
        await ValidarNomeUnicoAsync(nome, arena.Id, cancellationToken);

        arena.Nome = nome;
        arena.Slug = await ObterSlugUnicoAsync(nome, arena.Id, cancellationToken);
        arena.Descricao = Normalizar(request.Descricao);
        arena.TipoArena = request.TipoArena;
        arena.QuantidadeEspacos = request.QuantidadeEspacos;
        arena.Endereco = Normalizar(request.Endereco);
        arena.EnderecoResumo = Normalizar(request.EnderecoResumo);
        arena.Cidade = Normalizar(request.Cidade);
        arena.Estado = Normalizar(request.Estado);
        arena.Latitude = request.Latitude;
        arena.Longitude = request.Longitude;
        arena.Whatsapp = Normalizar(request.Whatsapp);
        arena.Instagram = Normalizar(request.Instagram);
        arena.Site = Normalizar(request.Site);
        arena.Publica = request.Publica;
        arena.PossuiIluminacao = request.PossuiIluminacao;
        arena.PossuiEstacionamento = request.PossuiEstacionamento;
        arena.PossuiVestiario = request.PossuiVestiario;
        arena.PossuiDucha = request.PossuiDucha;
        arena.PossuiBarRestaurante = request.PossuiBarRestaurante;
        arena.PossuiLoja = request.PossuiLoja;
        arena.PossuiCobertura = request.PossuiCobertura;
        arena.AtualizarDataModificacao();

        arenaRepositorio.Atualizar(arena);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return ParaDetalheAdmin(arena);
    }

    public async Task<IReadOnlyList<ArenaAdminResumoResponse>> ListarMinhasAsync(
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var incluirTodas = usuario.Perfil == PerfilUsuario.Administrador;
        var arenas = await arenaRepositorio.ListarAdministradasAsync(usuario.Id, incluirTodas, cancellationToken);

        return arenas.Select(x => new ArenaAdminResumoResponse(
            x.Id,
            x.Nome,
            x.Slug,
            x.TipoArena,
            x.Cidade,
            x.Estado,
            x.EnderecoResumo,
            x.LogoUrl,
            x.CapaUrl,
            x.Publica,
            x.Ativa,
            x.QuantidadeEspacos,
            x.Responsaveis.FirstOrDefault(r =>
                r.UsuarioId == usuario.Id &&
                r.Ativo &&
                r.Papel == PapelArenaResponsavel.ArenaAdmin)?.Papel))
            .ToList();
    }

    public async Task<ArenaAdminDetalheResponse> ObterAdminAsync(
        Guid arenaId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterAdminPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arenaId, cancellationToken);
        return ParaDetalheAdmin(arena);
    }

    public async Task AtualizarStatusAsync(
        Guid arenaId,
        bool ativa,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arenaId, cancellationToken);

        arena.Ativa = ativa;
        arena.AtualizarDataModificacao();
        arenaRepositorio.Atualizar(arena);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task AtualizarVisibilidadeAsync(
        Guid arenaId,
        bool publica,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arenaId, cancellationToken);

        arena.Publica = publica;
        arena.AtualizarDataModificacao();
        arenaRepositorio.Atualizar(arena);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArenaEspacoAdminResponse>> ListarEspacosAsync(
        Guid arenaId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arena.Id, cancellationToken);

        return (await arenaRepositorio.ListarEspacosPorArenaAsync(arena.Id, cancellationToken))
            .Select(ParaEspacoAdminResponse)
            .ToList();
    }

    public async Task<ArenaEspacoAdminResponse> CriarEspacoAsync(
        Guid arenaId,
        CriarArenaEspacoRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arena.Id, cancellationToken);

        var nome = ValidarNomeEspaco(request.Nome);
        ValidarTipoEspaco(request.TipoEspaco);
        ValidarOrdemExibicao(request.OrdemExibicao);

        var espaco = new ArenaEspaco
        {
            ArenaId = arena.Id,
            Nome = nome,
            TipoEspaco = request.TipoEspaco,
            Descricao = Normalizar(request.Descricao),
            PossuiIluminacao = request.PossuiIluminacao,
            PossuiCobertura = request.PossuiCobertura,
            Ativo = request.Ativo,
            OrdemExibicao = request.OrdemExibicao
        };

        await arenaRepositorio.AdicionarEspacoAsync(espaco, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return ParaEspacoAdminResponse(espaco);
    }

    public async Task<ArenaEspacoAdminResponse> AtualizarEspacoAsync(
        Guid arenaId,
        Guid espacoId,
        AtualizarArenaEspacoRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arena.Id, cancellationToken);

        var espaco = await arenaRepositorio.ObterEspacoPorIdEArenaAsync(arena.Id, espacoId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Espaço da arena não encontrado.");

        var nome = ValidarNomeEspaco(request.Nome);
        ValidarTipoEspaco(request.TipoEspaco);
        ValidarOrdemExibicao(request.OrdemExibicao);

        espaco.Nome = nome;
        espaco.TipoEspaco = request.TipoEspaco;
        espaco.Descricao = Normalizar(request.Descricao);
        espaco.PossuiIluminacao = request.PossuiIluminacao;
        espaco.PossuiCobertura = request.PossuiCobertura;
        espaco.Ativo = request.Ativo;
        espaco.OrdemExibicao = request.OrdemExibicao;
        espaco.AtualizarDataModificacao();

        arenaRepositorio.AtualizarEspaco(espaco);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return ParaEspacoAdminResponse(espaco);
    }

    public async Task AtualizarStatusEspacoAsync(
        Guid arenaId,
        Guid espacoId,
        bool ativo,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(arenaId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, arena.Id, cancellationToken);

        var espaco = await arenaRepositorio.ObterEspacoPorIdEArenaAsync(arena.Id, espacoId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Espaço da arena não encontrado.");

        espaco.Ativo = ativo;
        espaco.AtualizarDataModificacao();
        arenaRepositorio.AtualizarEspaco(espaco);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArenaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var arenas = await arenaRepositorio.ListarAsync(cancellationToken);
        return arenas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<ArenaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var arena = await arenaRepositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arena não encontrada.");
        await GarantirGestaoPermitidaAsync(usuario, id, cancellationToken);
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
            throw new AcessoNegadoException("O usuário só pode administrar arenas em que é responsável ativo.");
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
        var slugBase = GerarSlug(nome);
        var slug = slugBase;
        var sufixo = 2;

        while (await arenaRepositorio.ExisteSlugAsync(slug, idIgnorado, cancellationToken))
        {
            slug = $"{slugBase}-{sufixo++}";
        }

        return slug;
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

    private static void ValidarTipoEspaco(TipoEspaco tipoEspaco)
    {
        if (!Enum.IsDefined(tipoEspaco))
        {
            throw new RegraNegocioException("Tipo de espaço inválido.");
        }
    }

    private static string ValidarNomeEspaco(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do espaço é obrigatório.");
        }

        return nome.Trim();
    }

    private static void ValidarOrdemExibicao(int? ordemExibicao)
    {
        if (ordemExibicao.HasValue && ordemExibicao < 0)
        {
            throw new RegraNegocioException("Ordem de exibição não pode ser negativa.");
        }
    }

    private static void ValidarQuantidadeEspacos(int quantidadeEspacos)
    {
        if (quantidadeEspacos < 0)
        {
            throw new RegraNegocioException("Quantidade de espaços não pode ser negativa.");
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

    private static ArenaAdminDetalheResponse ParaDetalheAdmin(Arena arena)
        => new(
            arena.Id,
            arena.Nome,
            arena.Slug,
            arena.Descricao,
            arena.TipoArena,
            arena.Endereco,
            arena.EnderecoResumo,
            arena.Cidade,
            arena.Estado,
            arena.Latitude,
            arena.Longitude,
            arena.Whatsapp,
            arena.Instagram,
            arena.Site,
            arena.QuantidadeEspacos,
            arena.LogoUrl,
            arena.CapaUrl,
            arena.Publica,
            arena.Ativa,
            arena.PossuiIluminacao,
            arena.PossuiEstacionamento,
            arena.PossuiVestiario,
            arena.PossuiDucha,
            arena.PossuiBarRestaurante,
            arena.PossuiLoja,
            arena.PossuiCobertura,
            arena.Responsaveis
                .Where(x => x.Ativo && x.Usuario is not null)
                .Select(x => new ArenaResponsavelResponse(
                    x.UsuarioId,
                    x.Usuario!.Nome,
                    x.Usuario.Email,
                    x.Papel))
                .ToList());

    private static ArenaEspacoAdminResponse ParaEspacoAdminResponse(ArenaEspaco espaco)
        => new(
            espaco.Id,
            espaco.ArenaId,
            espaco.Nome,
            espaco.TipoEspaco,
            espaco.Descricao,
            espaco.PossuiIluminacao,
            espaco.PossuiCobertura,
            espaco.Ativo,
            espaco.OrdemExibicao);
}
