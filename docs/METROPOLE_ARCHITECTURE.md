# METRÓPOLE ∞ — Arquitetura 1.6.0

## Princípio

A verdade do mundo está em `Metropole.Sim`. Godot representa essa verdade visualmente, mas não é a fonte de identidade ou persistência dos cidadãos.

## Fluxo principal

```text
GameState
  ├─ Player
  ├─ Citizens
  ├─ Companies
  ├─ Markets
  ├─ Districts
  └─ LivingCity
       ├─ Properties
       ├─ Vehicles
       ├─ Regions
       ├─ Events
       └─ Metrics

SimulationEngine
  ├─ economia diária
  ├─ demografia
  ├─ empresas
  ├─ clima
  ├─ relações existentes
  └─ LivingCitySystems
       ├─ LOD populacional
       ├─ necessidades
       ├─ objetivos
       ├─ memória
       ├─ grafo social
       ├─ famílias
       ├─ imóveis
       ├─ tráfego abstrato
       ├─ spatial query
       └─ scheduler

Godot
  ├─ PremiumCityView
  │    ├─ MultiMesh de tráfego
  │    └─ MultiMesh de pedestres
  └─ CityView fallback
```

## LOD populacional

O nível de detalhe é calculado relativamente ao distrito atual do jogador.

- Interactive: cidadão explicitamente focado.
- Active: mesmo distrito do jogador.
- Regional: distrito adjacente.
- Abstract: restante da cidade.

A representação 3D não altera o dado persistente.

## Scheduler

`GetScheduledBatch(state, budget)` seleciona agentes vivos de forma rotativa, priorizando maior nível de detalhe. Isso impede atualizações detalhadas ilimitadas no mesmo ciclo.

Rotinas de alta frequência permanecem pequenas. Relações sociais detalhadas são processadas em cadência menor.

## Necessidades e objetivos

Necessidades ampliadas:

- fome;
- energia;
- sede;
- higiene;
- banheiro;
- social;
- conforto;
- segurança;
- saúde.

O maior nível de urgência produz um objetivo, e o objetivo procura uma affordance adequada no catálogo data-driven.

## Affordances

`LivingCityCatalog` descreve ações oferecidas por objetos e serviços, incluindo efeito, duração, custo, risco e prioridade.

Isso desacopla a IA de tipos concretos de objetos.

## Relações

Cada ligação social pode conter:

- familiaridade;
- amizade;
- confiança;
- atração;
- respeito;
- ressentimento;
- romance.

O modelo permite sentimentos simultâneos e contraditórios.

## Memória

Memórias possuem tipo, participantes, dia, peso emocional, importância, decaimento e resumo. Memórias pouco importantes desaparecem; as mais relevantes persistem por mais tempo.

## Família e moradia

Households existentes são aproveitados para derivar relações entre adultos e menores. Imóveis são criados deterministicamente por domicílio e mantêm proprietário, residentes, valor, aluguel, condição e número de cômodos.

## Veículos e tráfego

Veículos pertencem a cidadãos. O destino alterna entre residência e emprego conforme a atividade do proprietário.

A simulação lógica registra distrito atual, destino e progresso da rota. O renderer premium interpola proxies visuais a partir desses dados.

## Regiões

Cada distrito possui snapshot regional com:

- população;
- agentes ativos;
- regionais;
- abstratos;
- veículos;
- carga de tráfego;
- atividade econômica.

## Persistência

`SaveStore` inicializa Living City antes de salvar e depois de carregar. Isso torna campos novos compatíveis com saves anteriores do schema atual.

## Determinismo

A inicialização usa a seed mundial e IDs persistentes. Sistemas que não precisam de aleatoriedade evitam RNG. O objetivo é permitir reprodução de bugs e testes multi-seed.

## Rendering

O renderer 3D não instancia 1.200 personagens como Nodes. MultiMesh e proxies limitados continuam sendo a estratégia principal para densidade urbana.

O custo visual pode diminuir por perfil gráfico sem diminuir a população lógica.
