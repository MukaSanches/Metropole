# Arquitetura — METRÓPOLE ∞ 1.0.0

## Decisão de motor

A V1 usa **Godot Engine 4.7.2 stable .NET + C#/.NET 8**.

Razões:
- 4.7.2 é uma versão estável atual, enquanto 4.8 está em desenvolvimento;
- exportação oficial para Windows x86_64;
- suporte oficial a C#;
- licença MIT compatível com distribuição comercial, com atribuição;
- UI 2D/2.5D, desenho procedural e integração desktop adequados ao produto;
- núcleo C# pode ser testado sem iniciar o editor.

## Separação

`Metropole.Sim` não depende de Godot. Contém:
- Core / RNG determinístico;
- World / geração por seed;
- Population / cidadãos e famílias simplificadas;
- Careers / vagas, contratação e salários;
- Companies / caixa, dívida, produção e falência;
- Economy / mercados, preços e transferências monetárias;
- Events / histórico causal;
- Persistence / save versionado e atômico.

`Metropole.Game` contém:
- UI;
- visualização isométrica;
- loop de apresentação;
- integração de save com a pasta de dados do usuário.

## Determinismo

O estado inicial depende de `seed + versão das regras`. O núcleo usa um PRNG próprio, não o FPS. O tempo é avançado em ticks de dia explícitos.

## Simulação hierárquica

- L0: jogador — atualizado a cada dia.
- L1: cidadãos do distrito do jogador — necessidades atualizadas diariamente.
- L2: demais cidadãos — necessidades sociais agregadas em cadência semanal.
- L2: empresas e mercados — transações econômicas diárias.
- L4: estatísticas de cidade — recalculadas em agregados.

A materialização visual é limitada a uma fração das entidades; a simulação econômica não depende da quantidade desenhada.

## Economia e conservação

Transferências principais têm contraparte:
- salário: empresa → pessoa;
- consumo: pessoa → empresa;
- aluguel/impostos: pessoa/empresa → tesouro agregado;
- crédito: tesouro/banco agregado → empresa, com dívida registrada;
- aporte do jogador: jogador → empresa.

Produção altera estoque, não cria moeda. O teste de conservação mede dinheiro líquido em operações sem criação destrutiva.

## Conteúdo composicional

Catálogos combinam raízes semânticas, setores, especializações, formatos, materiais e níveis. As combinações retornadas são usadas diretamente por vagas, empresas, mercados e tecnologias; não são números decorativos.

## Save

O save usa JSON versionado. A escrita é:
1. serializar para arquivo temporário;
2. validar desserialização;
3. copiar save anterior para `.bak`;
4. substituir o arquivo principal.

Um schema futuro desconhecido é rejeitado de forma explícita.
