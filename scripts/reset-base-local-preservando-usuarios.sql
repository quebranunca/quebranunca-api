BEGIN;

DO $$
DECLARE
    quantidade_usuarios_preservados integer;
BEGIN
    SELECT COUNT(*)
    INTO quantidade_usuarios_preservados
    FROM usuarios
    WHERE email IN ('admin@teste.com', 'organizador@teste.com', 'atleta@teste.com');

    IF quantidade_usuarios_preservados <> 3 THEN
        RAISE EXCEPTION
            'Reset cancelado: esperados 3 usuarios preservados (admin@teste.com, organizador@teste.com, atleta@teste.com), mas foram encontrados %.',
            quantidade_usuarios_preservados;
    END IF;
END $$;

UPDATE usuarios
SET atleta_id = NULL,
    codigo_login_hash = NULL,
    codigo_login_expira_em_utc = NULL,
    codigo_redefinicao_senha_hash = NULL,
    codigo_redefinicao_senha_expira_em_utc = NULL,
    refresh_token_hash = NULL,
    refresh_token_expira_em_utc = NULL,
    data_atualizacao = timezone('utc', now())
WHERE email IN ('admin@teste.com', 'organizador@teste.com', 'atleta@teste.com');

DELETE FROM partidas_aprovacoes;
DELETE FROM pendencias_usuarios;
DELETE FROM partidas;
DELETE FROM inscricoes_campeonato;
DELETE FROM grupos_atletas;
DELETE FROM categorias_competicao;
DELETE FROM competicoes;
DELETE FROM convites_cadastro;
DELETE FROM duplas;
DELETE FROM atletas;
DELETE FROM regras_competicao;
DELETE FROM formatos_campeonato;
DELETE FROM locais;
DELETE FROM ligas;

DELETE FROM usuarios
WHERE email NOT IN ('admin@teste.com', 'organizador@teste.com', 'atleta@teste.com');

COMMIT;
