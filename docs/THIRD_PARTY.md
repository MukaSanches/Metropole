# METRÓPOLE ∞ — Dependências e referências

## Incluídos na distribuição

### Godot Engine

Licença: MIT.

Usado como engine de rendering, UI, áudio e plataforma de export.

### Kenney

Assets selecionados CC0 já utilizados pela linha 1.4/1.5 e preservados na 1.6.

Os arquivos e hashes são adquiridos pelo pipeline pinado existente.

### OpenGameArt

Ambiências CC0 selecionadas e já documentadas em `THIRD_PARTY_NOTICES.md`.

## Pesquisa arquitetural — sem código incorporado nesta release

Os projetos abaixo foram estudados como referência conceitual. Código deles não foi copiado para o núcleo Living City 1.6:

- Veloren — arquitetura ECS, simulação distante e mundo persistente; GPL.
- OpenRCT2 — agentes, pensamentos, filas e grandes populações; GPL.
- OpenTTD — redes, rotas, economia e transporte; GPL.
- OpenMW — mundo aberto, persistência e sistemas de RPG; GPL.
- Unknown Horizons — economia e cadeias produtivas; GPL.
- FreeSO — affordances e comportamento orientado por objetos; MPL.
- Cataclysm: DDA — sistemas data-driven, itens e necessidades; licença própria do projeto/conteúdo, tratada somente como referência.

## Plugins avaliados, não incorporados em 1.6

- Terrain3D;
- Godot Road Generator;
- LimboAI;
- Godot State Charts;
- Dialogue Manager;
- GLoot;
- Sky3D;
- ProtonScatter / Foliage3D.

A não inclusão é intencional: esta release prioriza estabilidade e arquitetura própria. Qualquer plugin futuro deve ter versão, commit, licença, finalidade e testes registrados antes de entrar no produto.

## Código Living City

`src/Metropole.Sim/LivingCitySystems.cs` e os modelos associados foram implementados especificamente para METRÓPOLE 1.6 e não dependem de código copyleft de terceiros.
