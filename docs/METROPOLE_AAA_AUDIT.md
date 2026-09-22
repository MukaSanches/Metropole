# METRÓPOLE ∞ — Auditoria AAA 1.6

## Baseline
- Godot 4.7.2 .NET, C# e .NET 8.
- Simulação determinística separada do renderer.
- Geração padrão com 1.200 cidadãos, 180 empresas e 9 distritos.
- Renderer premium, fallback leve, áudio, vida social e instalador já existentes na linha 1.5.

## Decisão
A 1.6 preserva a arquitetura existente e adiciona profundidade no núcleo C# em vez de substituir sistemas estáveis por vários plugins simultaneamente.

## Entregue na 1.6
- LOD Statistical, Regional, Active e Interactive;
- scheduler por lotes;
- necessidades e personalidade persistentes;
- memória com decay;
- utility goals;
- affordances;
- domicílios, imóveis e veículos;
- tráfego abstrato e regiões agregadas;
- orçamento de performance;
- migração de save.

Dependências externas só devem entrar quando houver ganho mensurável, licença compatível e teste de regressão.
