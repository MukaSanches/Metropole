# METRÓPOLE ∞ 1.6 — Systemic Life

## Objetivo

A 1.6 transforma a base 1.5 em uma simulação de vida com múltiplos níveis de detalhe, decisões por utilidade e relações persistentes sem acoplar milhares de cidadãos a Nodes 3D.

## Sistemas implementados

### Population LOD

Cada cidadão lógico recebe um nível de detalhe:

- Interactive: cidadãos mais relevantes no distrito do jogador, orçamento padrão de 24;
- Active: cidadãos locais adicionais, orçamento total padrão de 96 entre Interactive + Active;
- Regional: cidadãos do distrito do jogador e vizinhos fora do orçamento ativo;
- Abstract: restante da cidade.

O nível visual pode continuar separado. A verdade persistente permanece em `Metropole.Sim`.

### Utility AI

Cidadãos avaliam objetivos por pontuação:

- trabalho;
- estudo;
- sono;
- alimentação;
- higiene;
- socialização;
- lazer;
- rotina doméstica/deslocamento.

As decisões consideram energia, fome, higiene, sociabilidade, diversão, disciplina, ambição e horário.

### Necessidades

Além de fome, energia, felicidade e estresse existentes, cidadãos passam a possuir:

- saúde;
- higiene;
- necessidade social;
- diversão;
- conforto;
- objetivo atual.

O jogador recebe higiene, diversão, conforto, segurança e objetivo atual.

### Affordances

Objetos/atividades são representados por ações contextuais data-driven:

- cama → dormir;
- alimentação → comer;
- chuveiro → higiene;
- sofá/TV → relaxar;
- celular/espaço social → socializar;
- computador/estudo → estudar.

A interface consulta o catálogo em vez de depender somente de botões específicos.

### Grafo social

Relações são persistidas separadamente de um simples estado de parceiro:

- familiaridade;
- amizade;
- confiança;
- atração;
- respeito;
- ressentimento;
- romance.

Interações sociais são limitadas por orçamento e indexadas por par de cidadãos para manter custo controlado.

### Memórias

Eventos sociais relevantes podem gerar memórias com:

- dia;
- tipo;
- peso emocional;
- importância;
- decaimento.

Memórias triviais decaem e a coleção possui limite para evitar crescimento ilimitado.

### Moradias lógicas

Households agora geram `ResidenceState` persistente com:

- distrito;
- moradores;
- capacidade;
- qualidade;
- aluguel mensal de referência.

### Save schema 2

A 1.6 usa schema 2. Saves schema 1 da 1.5 são migrados automaticamente durante carregamento/salvamento. Backups atômicos existentes foram preservados.

## Frequência de simulação

O LOD também controla cadence:

- Interactive / Active: atualização horária;
- Regional: a cada 2 horas com delta agregado;
- Abstract: a cada 4 horas com delta agregado.

Isso reduz trabalho sem congelar a vida fora da câmera.

## QA

A 1.6 adiciona testes para:

- fechamento das classes de LOD;
- limite de cidadãos interativos/ativos;
- Utility AI;
- relações sociais;
- affordances;
- conservação monetária em interação de alimentação;
- necessidades em intervalos válidos;
- migração de schema 1 para 2;
- moradias lógicas.

Os testes econômicos anteriores, matriz multi-seed de 5 anos, save/load e long-run permanecem.

## Dependências

Nenhuma biblioteca externa nova foi incorporada ao core da 1.6. Os conceitos estudados em projetos open source foram reimplementados sobre a arquitetura C# existente, evitando impor novas obrigações de licença ao jogo.
