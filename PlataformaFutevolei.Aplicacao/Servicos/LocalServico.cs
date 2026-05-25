using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

// Adaptador temporario para clientes que ainda consomem /api/locais.
public class LocalServico(IArenaServico arenaServico) : ILocalServico
{
    public async Task<IReadOnlyList<LocalDto>> ListarAsync(CancellationToken cancellationToken = default)
        => (await arenaServico.ListarAsync(cancellationToken)).Select(ParaLocalDto).ToList();

    public async Task<LocalDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ParaLocalDto(await arenaServico.ObterPorIdAsync(id, cancellationToken));

    public async Task<LocalDto> CriarAsync(CriarLocalDto dto, CancellationToken cancellationToken = default)
        => ParaLocalDto(await arenaServico.CriarAsync(new CriarArenaDto(
            dto.Nome,
            null,
            ParaTipoArena(dto.Tipo),
            dto.QuantidadeQuadras,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true,
            true), cancellationToken));

    public async Task<LocalDto> AtualizarAsync(Guid id, AtualizarLocalDto dto, CancellationToken cancellationToken = default)
    {
        var arena = await arenaServico.ObterPorIdAsync(id, cancellationToken);
        return ParaLocalDto(await arenaServico.AtualizarAsync(id, new AtualizarArenaDto(
            dto.Nome,
            arena.Descricao,
            ParaTipoArena(dto.Tipo),
            dto.QuantidadeQuadras,
            arena.Endereco,
            arena.EnderecoResumo,
            arena.Cidade,
            arena.Estado,
            arena.Latitude,
            arena.Longitude,
            arena.Whatsapp,
            arena.Instagram,
            arena.Site,
            arena.LogoUrl,
            arena.LogoPublicId,
            arena.CapaUrl,
            arena.CapaPublicId,
            arena.Publica,
            arena.Ativa), cancellationToken));
    }

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
        => arenaServico.RemoverAsync(id, cancellationToken);

    private static LocalDto ParaLocalDto(ArenaDto arena)
        => new(
            arena.Id,
            arena.Nome,
            arena.TipoArena switch
            {
                TipoArena.RedePraia => TipoLocal.RedeGrupoAmigos,
                TipoArena.Escola => TipoLocal.RedeEscolaFutevolei,
                TipoArena.Temporaria => TipoLocal.ArenaTemporaria,
                _ => TipoLocal.ArenaParticular
            },
            arena.QuantidadeEspacos,
            arena.UsuarioResponsavelId,
            arena.NomeUsuarioResponsavel,
            arena.DataCriacao,
            arena.DataAtualizacao);

    private static TipoArena ParaTipoArena(TipoLocal tipo)
        => tipo switch
        {
            TipoLocal.ArenaParticular => TipoArena.ArenaPrivada,
            TipoLocal.RedeGrupoAmigos => TipoArena.RedePraia,
            TipoLocal.RedeEscolaFutevolei => TipoArena.Escola,
            TipoLocal.ArenaTemporaria => TipoArena.Temporaria,
            _ => throw new RegraNegocioException("Tipo de local inválido.")
        };
}
