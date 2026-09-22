# METRÓPOLE ∞ — Auditoria AAA 1.6.0

Data: 2026-09-22

## Baseline preservado

A versão 1.5.0 já possuía uma base de produção relevante: simulação determinística em C# separada do rendering, 1.200 cidadãos, aproximadamente 180 empresas, nove distritos, emprego, salários, consumo, mercado, concorrência, relacionamentos básicos, clima, passagem de tempo, sucessão, save local, renderer Godot Forward+ com fallback Compatibility, MultiMesh, assets CC0 pinados, export Windows e instalador Inno Setup.

A estratégia 1.6 foi, portanto, evoluir a arquitetura existente em vez de substituí-la.

## Lacunas encontradas no prompt mestre

Os principais pontos ausentes ou pouco estruturados eram:

- níveis explícitos de detalhe da simulação populacional;
- necessidades além de fome e energia;
- personalidade persistente expandida;
- memória social;
- relacionamentos multidimensionais;
- estrutura familiar persistente;
- propriedades ligadas a domicílios;
- frota persistente ligada aos cidadãos;
- abstração regional de tráfego e atividade;
- scheduler com orçamento de atualizações;
- consultas espaciais sem varrer toda a população;
- affordances orientadas a dados;
- objetivos derivados de necessidades;
- ligação mais direta entre simulação lógica e proxies 3D.

## Implementado em 1.6.0

A camada Living City foi adicionada ao núcleo C# sem depender da SceneTree do Godot.

Cada cidadão agora mantém dados persistentes de necessidades ampliadas, personalidade, memórias, relações, vínculos familiares, casa, objetivo, ação planejada e nível de simulação.

A população é classificada em quatro níveis:

1. Abstract — cidadão distante, mantido como dados.
2. Regional — cidadão em região próxima, atualizado em cadência reduzida.
3. Active — cidadão no distrito do jogador.
4. Interactive — cidadão explicitamente focado para interação detalhada.

O scheduler distribui atualizações por orçamento e os sistemas usam cadências diferentes em vez de recalcular tudo a cada frame.

## Rendering

O renderer premium continua usando MultiMesh e proxies limitados. Na 1.6, tráfego e pedestres premium deixam de depender apenas de elementos decorativos: a posição e quantidade dos proxies passam a ser dirigidas pelos cidadãos, veículos, distritos, atividades e níveis de detalhe persistentes.

O fallback 2D da 1.5 foi preservado sem reescrita de alto risco.

## Persistência

Os novos campos foram projetados de forma aditiva. Saves do schema 1 continuam sendo aceitos e a camada Living City inicializa dados ausentes deterministicamente na carga.

Nenhum fallback destrutivo foi adicionado.

## Dependências externas

Não foram adicionados Terrain3D, LimboAI, Dialogue Manager, GLoot ou outros plugins somente para satisfazer uma lista. A arquitetura atual já possui um núcleo C# independente e a introdução simultânea desses plugins aumentaria risco de regressão, incompatibilidade e dívida de integração.

Eles permanecem candidatos a fases posteriores, sujeitos a benchmark e integração isolada.

Projetos copyleft estudados não tiveram código copiado para o núcleo.

## Critério de release

A build 1.6 somente deve ser considerada pronta quando o GitHub Actions confirmar:

- build Release de Metropole.Sim;
- suíte de testes de simulação;
- aquisição/verificação dos assets CC0;
- build C# do Godot;
- importação Godot;
- export Windows;
- validação automática da UI;
- validação da camada premium;
- validação do fallback;
- smoke test do executável;
- geração do instalador;
- instalação silenciosa;
- execução pós-instalação;
- SHA-256;
- upload do artifact Windows.
