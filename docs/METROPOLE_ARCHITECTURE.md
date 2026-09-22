# METRÓPOLE ∞ — Arquitetura AAA 1.6

```text
Metropole.Game
  UI / render / áudio / interação
          |
          v
Metropole.Sim
  economia + social + AAA Simulation Core
          |
          +-- AaaWorldState
          +-- CitizenSimulationProfile
          +-- SchedulerState
          +-- RegionSimulationState
          +-- TrafficLinkState
          +-- Household / Property / Vehicle
          +-- AffordanceCatalog
```

## Population LOD
- Statistical: estado lógico de baixo custo.
- Regional: região próxima sem proxy obrigatório.
- Active: mesmo distrito do jogador, limitado por orçamento.
- Interactive: cidadão selecionado diretamente.

## Scheduler
O relógio horário processa lotes determinísticos. Agregações mais caras usam cadência controlada e não alteram a verdade econômica.

## Utility e affordances
As pressões de necessidades geram um objetivo de maior utilidade. Objetos são representados por ações disponíveis em catálogo, permitindo expansão sem acoplar a IA central a cada objeto.

## Persistência
Schema 2 com migração do schema 1 e reconstrução determinística do estado AAA ausente em saves antigos.
