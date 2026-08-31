# RevisarService

Revise o service informado.

Verifique:

1. Regras de negócio estão centralizadas no local correto.
2. Não existe lógica relevante em controller.
3. Ownership e autorização estão sendo validados.
4. Mensagens de domínio estão em português e consistentes.
5. Existe reutilização adequada de repositories e services existentes.
6. Não há duplicação de regras já existentes no sistema.
7. Não existe bypass direto de persistência ignorando abstrações do projeto.
8. Fluxo respeita as regras de Grupo, Competição, Liga, Arena e Partida.
9. Valide impactos em:
   - ranking
   - histórico
   - pendências
   - aprovações
   - convites
   - dashboards
10. Identifique efeitos colaterais não óbvios.
11. Avalie necessidade de testes de aplicação e domínio.
12. Liste riscos funcionais e técnicos antes de sugerir alterações.

Ao final apresente:

- Problemas encontrados
- Riscos
- Melhorias sugeridas
- Testes recomendados
- Impacto no domínio