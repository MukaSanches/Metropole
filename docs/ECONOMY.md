# Modelo econômico da V1

A V1 é uma simulação econômica discreta por dia.

## Fluxos
- empresas pagam salários;
- cidadãos distribuem consumo entre mercados conforme renda, necessidade e preço;
- empresas produzem estoque conforme mão de obra e produtividade;
- mercados ajustam preços de forma limitada pela relação demanda/estoque;
- empresas com caixa insuficiente buscam crédito limitado;
- perdas persistentes levam a redução de quadro e, por fim, insolvência;
- falências reduzem oferta e podem pressionar preço;
- oportunidade de demanda pode estimular abertura de novas empresas.

## Estabilidade

O preço é limitado a uma faixa em torno do preço-base e usa ajuste amortecido. Estoque nunca pode ser negativo. Valores NaN/infinito causam falha nos testes.

## Causalidade

Eventos relevantes registram uma cadeia textual curta, por exemplo:

`demanda caiu → receita insuficiente → caixa negativo → crédito esgotado → insolvência`.

O histórico é compactado quando supera o limite configurado.
