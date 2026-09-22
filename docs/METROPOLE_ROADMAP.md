# METRÓPOLE ∞ — Roadmap técnico após 1.6.0

Este documento diferencia o que está implementado do que ainda requer uma integração maior.

## 1.6.0 — Living City Core

Entregue no código:

- LOD de simulação em quatro níveis;
- scheduler com orçamento;
- consultas espaciais por distrito;
- necessidades ampliadas;
- personalidade persistente;
- affordances data-driven;
- objetivos e ações planejadas;
- memórias;
- relações multidimensionais;
- vínculos familiares;
- imóveis por domicílio;
- veículos persistentes;
- tráfego abstrato casa/trabalho;
- métricas regionais;
- event stream;
- save/load compatível;
- proxies premium ligados à simulação;
- testes de regressão e validação exportada.

## Próxima expansão — interação física

- transformar affordances lógicas em objetos 3D selecionáveis;
- animações contextuais de sentar, dormir, usar objetos e entrar em veículos;
- interiores carregados sob demanda;
- inventário físico e containers;
- interface de inspeção detalhada de cidadãos.

Critério: nenhuma ação deve existir somente como botão sem consequência simulada.

## Próxima expansão — malha viária

Avaliar um road graph próprio ou integração isolada com Godot Road Generator.

Antes de integrar:

1. benchmark;
2. verificar licença/versão;
3. protótipo em branch isolada;
4. validar export Windows;
5. provar vantagem sobre a malha atual.

## Próxima expansão — terreno e streaming

Avaliar Terrain3D somente quando o escopo exigir uma cidade maior que a grade atual.

O objetivo é streaming regional e LOD de mundo, não trocar terreno por estética.

## Próxima expansão — IA visual

Avaliar LimboAI para agentes realmente materializados em 3D. A simulação abstrata continuará no núcleo C#.

Behavior Trees não devem substituir o sistema de dados persistentes.

## Próxima expansão — diálogo

Avaliar Dialogue Manager ou solução própria para diálogos condicionais ligados ao estado de relações, memória e personalidade.

## Próxima expansão — inventário

Avaliar GLoot ou uma implementação C# data-driven. A decisão depende de compatibilidade com .NET/Godot e custo de integração.

## Performance

Manter a regra:

```text
simular muito
renderizar apenas o necessário
```

Toda ampliação deve preservar:

- profiling;
- budgets por sistema;
- regressão;
- stress tests;
- fallback gráfico;
- save versionado;
- build Windows automatizada.
