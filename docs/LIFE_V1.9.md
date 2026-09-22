# Cotidiano 1.9 — contrato de simulação

Fonte de verdade: `EverydayLifeState`, persistido em `GameState.Life` (schema 3).
`EverydayLife` contém catálogo, pré-condições, execução, relógio, diário e validação.
`CitizenDecisions` avalia um conjunto fixo de alternativas por cidadão e hora, sem buscar outros cidadãos; desempate determinístico e bônus de continuidade de atividade.
`Main.EverydayLife` apenas apresenta o estado e chama comandos.

## Tempo e dinheiro
A execução cobra antes de avançar as horas; cada hora usa o mesmo motor de economia/vida existente. Falhas de pré-condição não alteram o mundo. Uma rotina para na primeira ação indisponível e informa quantas foram concluídas; não desfaz ações anteriores. O tick usa a diferença de horas absolutas para não cobrar duas vezes na meia-noite. Avanço direto de dias atualiza as novas necessidades.

Os custos de serviços e ingredientes vão para a tesouraria como aproximação econômica; a nova culinária ainda não retira ingredientes do estoque de empresas. Competências novas acumulam prática e marcos, mas não criam salário ou carreira novos automaticamente.

## Persistência e limites
Schemas 1/2 recebem estado inicial saudável e migram para 3; saves não são rebaixados. O mecanismo existente mantém arquivo temporário e backup. Diário limitado a 64 entradas; eventos pendentes limitados a um e novos convites no máximo semanais. Sucessão preserva o diário familiar e condição da casa.

## Validação
Seis grupos novos em Metropole.SimTests: ações/dinheiro/bloqueios; rotinas/prática; meia-noite/avanço diário/limites; JSON legado sem Life e rejeição de save inválido; decisões em situações controladas; determinismo e eventos. Workflow Windows V1.9 executa também regressão de economia, família, save, longa duração, importação Godot, UI em quatro resoluções, exportação e instalação.
