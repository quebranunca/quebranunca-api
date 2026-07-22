# Pontos QN

## Consolidação de atletas

- A consolidação deve preservar saldo, extrato e resgates de Pontos QN antes de remover o atleta duplicado.
- O extrato do atleta perdedor é transferido para o vencedor dentro da mesma transação da consolidação.
- Chaves de idempotência que identificam o atleta são normalizadas para o vencedor, preservando as exceções históricas de saldo inicial retroativo e estorno de partida.
- Duplicidades exatas podem ser consolidadas somente quando os campos contábeis e vínculos relevantes forem equivalentes e nenhum lançamento envolvido possuir estorno relacionado.
- Colisões ambíguas de movimentações causam rollback integral; não há escolha ou correção silenciosa.
- Resgates preservam identidade, status, datas, cupom, observações e vínculos. A consolidação não cancela, aprova, rejeita ou utiliza resgates.
- O estoque do benefício não é alterado durante a consolidação.
- O saldo final do vencedor é reconstruído a partir do extrato consolidado, que é a fonte de verdade contábil.
