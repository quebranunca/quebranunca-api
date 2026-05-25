using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ArenaServicoPublicoTests
{
    [Fact]
    public async Task ListarPublicasAsync_RetornaSomenteArenasPublicasAtivasOrdenadas()
    {
        var cenario = new Cenario();

        var arenas = await cenario.Servico.ListarPublicasAsync(new(null, null, null, null));

        Assert.Equal(["Arena A", "Arena B"], arenas.Select(x => x.Nome).ToArray());
    }

    [Theory]
    [InlineData(" Santos ", null, null, null, "Arena A")]
    [InlineData(null, " RJ ", null, null, "Arena B")]
    [InlineData(null, null, TipoArena.Clube, null, "Arena B")]
    [InlineData(null, null, null, " santos ", "Arena A")]
    public async Task ListarPublicasAsync_AplicaFiltrosInformados(
        string? cidade,
        string? estado,
        TipoArena? tipoArena,
        string? termoBusca,
        string arenaEsperada)
    {
        var cenario = new Cenario();

        var arenas = await cenario.Servico.ListarPublicasAsync(
            new(cidade, estado, tipoArena, termoBusca));

        Assert.Single(arenas);
        Assert.Equal(arenaEsperada, arenas[0].Nome);
    }

    [Fact]
    public async Task ObterPublicaPorSlugAsync_AceitaSlugSemDiferenciarMaiusculas()
    {
        var cenario = new Cenario();

        var arena = await cenario.Servico.ObterPublicaPorSlugAsync("ARENA-A");

        Assert.Equal("Arena A", arena.Nome);
    }

    [Theory]
    [InlineData("inexistente")]
    [InlineData("arena-privada")]
    [InlineData("arena-inativa")]
    public async Task ObterPublicaPorSlugAsync_RecusaArenaAusenteOuNaoPublica(string slug)
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterPublicaPorSlugAsync(slug));

        Assert.Equal("Arena não encontrada.", excecao.Message);
    }

    [Fact]
    public async Task ObterResumoPublicoAsync_RecusaArenaPrivada()
    {
        var cenario = new Cenario();

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterResumoPublicoAsync(cenario.ArenaPrivada.Id));
    }

    private sealed class Cenario
    {
        public Cenario()
        {
            ArenaPrivada = CriarArena("Arena Privada", "arena-privada", "Santos", "SP", TipoArena.ArenaPrivada, publica: false);
            var arenas = new List<Arena>
            {
                CriarArena("Arena B", "arena-b", "Rio de Janeiro", "RJ", TipoArena.Clube),
                CriarArena("Arena A", "arena-a", "Santos", "SP", TipoArena.Praia, descricao: "Praia paulista"),
                ArenaPrivada,
                CriarArena("Arena Inativa", "arena-inativa", "Santos", "SP", TipoArena.Praia, ativa: false)
            };

            Servico = new ArenaServico(
                new ArenaRepositorioMemoria(arenas),
                new ArenaResponsavelRepositorioStub(),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub());
        }

        public ArenaServico Servico { get; }
        public Arena ArenaPrivada { get; }

        private static Arena CriarArena(
            string nome,
            string slug,
            string cidade,
            string estado,
            TipoArena tipoArena,
            bool publica = true,
            bool ativa = true,
            string? descricao = null)
            => new()
            {
                Nome = nome,
                Slug = slug,
                Cidade = cidade,
                Estado = estado,
                TipoArena = tipoArena,
                Descricao = descricao,
                QuantidadeEspacos = 2,
                Publica = publica,
                Ativa = ativa
            };
    }

    private sealed class ArenaRepositorioMemoria(IReadOnlyList<Arena> arenas) : IArenaRepositorio
    {
        public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
            ArenaFiltroPublicoRequest filtro,
            CancellationToken cancellationToken = default)
        {
            var consulta = arenas.Where(x => x.Ativa && x.Publica);

            if (filtro.Cidade is not null)
            {
                consulta = consulta.Where(x => string.Equals(x.Cidade, filtro.Cidade, StringComparison.OrdinalIgnoreCase));
            }

            if (filtro.Estado is not null)
            {
                consulta = consulta.Where(x => string.Equals(x.Estado, filtro.Estado, StringComparison.OrdinalIgnoreCase));
            }

            if (filtro.TipoArena.HasValue)
            {
                consulta = consulta.Where(x => x.TipoArena == filtro.TipoArena.Value);
            }

            if (filtro.TermoBusca is not null)
            {
                consulta = consulta.Where(x =>
                    x.Nome.Contains(filtro.TermoBusca, StringComparison.OrdinalIgnoreCase) ||
                    (x.Cidade?.Contains(filtro.TermoBusca, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Estado?.Contains(filtro.TermoBusca, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var resultado = consulta
                .OrderBy(x => x.Nome)
                .Select(x => new ArenaListagemPublicaResponse(
                    x.Id,
                    x.Nome,
                    x.Slug,
                    x.Descricao,
                    x.TipoArena,
                    x.Cidade,
                    x.Estado,
                    x.EnderecoResumo,
                    x.QuantidadeEspacos,
                    x.LogoUrl,
                    x.CapaUrl,
                    x.Instagram,
                    x.Whatsapp,
                    x.Publica,
                    x.Ativa))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>(resultado);
        }

        public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var arena = arenas.FirstOrDefault(x =>
                x.Ativa && x.Publica && string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
            var response = arena is null
                ? null
                : new ArenaDetalhePublicoResponse(
                    arena.Id,
                    arena.Nome,
                    arena.Slug,
                    arena.Descricao,
                    arena.TipoArena,
                    arena.Cidade,
                    arena.Estado,
                    arena.Endereco,
                    arena.EnderecoResumo,
                    arena.Latitude,
                    arena.Longitude,
                    arena.Whatsapp,
                    arena.Instagram,
                    arena.Site,
                    arena.QuantidadeEspacos,
                    arena.LogoUrl,
                    arena.CapaUrl,
                    arena.Publica,
                    arena.Ativa);
            return Task.FromResult(response);
        }

        public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var arena = arenas.FirstOrDefault(x => x.Id == id && x.Ativa && x.Publica);
            var response = arena is null
                ? null
                : new ArenaResumoPublicoResponse(
                    arena.Id,
                    arena.Nome,
                    arena.Slug,
                    arena.TipoArena,
                    arena.Cidade,
                    arena.Estado,
                    arena.EnderecoResumo,
                    arena.LogoUrl,
                    arena.QuantidadeEspacos);
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(
            Guid usuarioId,
            bool incluirTodas,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Arena>>([]);

        public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Arena?>(null);

        public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(arenas);

        public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.FirstOrDefault(x => x.Id == id));

        public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.FirstOrDefault(x => x.Nome == nome));

        public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.Any(x => x.Slug == slug && x.Id != idIgnorado));

        public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(
            Guid arenaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArenaEspaco>>([]);

        public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(
            Guid arenaId,
            Guid espacoId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ArenaEspaco?>(null);

        public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Atualizar(Arena arena)
        {
        }

        public void AtualizarEspaco(ArenaEspaco espaco)
        {
        }

        public void Remover(Arena arena)
        {
        }
    }

    private sealed class ArenaResponsavelRepositorioStub : IArenaResponsavelRepositorio
    {
        public Task<bool> UsuarioPodeGerenciarAsync(Guid arenaId, Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AdicionarAsync(ArenaResponsavel responsavel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task ExecutarEmTransacaoAsync(
            Func<CancellationToken, Task> operacao,
            CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(null);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
