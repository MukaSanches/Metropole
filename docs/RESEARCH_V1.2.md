# Pesquisa de design — METRÓPOLE ∞ 1.2

Pesquisa realizada em 21/09/2026. O objetivo foi identificar padrões sólidos de simuladores de vida, cidade e negócios e reinterpretá-los para o METRÓPOLE sem copiar código, arte ou conteúdo proprietário.

## Godot 4.7

Fontes oficiais consultadas:

- https://docs.godotengine.org/en/4.7/
- https://docs.godotengine.org/en/4.7/engine_details/architecture/internal_rendering_architecture.html
- https://docs.godotengine.org/en/4.7/classes/class_navigationserver2d.html
- https://docs.godotengine.org/en/4.7/classes/class_gpuparticles2d.html

Decisões:
- manter o Compatibility renderer para alcance em PCs modestos;
- usar desenho procedural 2D com CanvasItem para cidade e efeitos;
- limitar redraw visual a 30 Hz para reduzir custo de CPU/GPU;
- usar antialiasing nas linhas de maior importância;
- evitar dependência do NavigationServer2D neste ciclo, pois o sistema é marcado como experimental e os pedestres desta versão são ambientação visual, não agentes físicos completos;
- manter a simulação independente do FPS.

## Software Inc.

Fonte:
- https://store.steampowered.com/app/362620/Software_Inc/
- https://www.softwareinc.coredumping.com/wiki/index.php/Main_Page

Padrões estudados:
- funcionários com necessidades, habilidades e satisfação;
- equipes, especializações e salários;
- produtos, marketing, pesquisa e suporte;
- competição simulada e histórico emergente;
- delegação e operação empresarial profunda.

Aplicado na 1.2:
- moral, salário, produtividade, inovação, marketing, estratégia e rivalidade;
- cidadãos trocam de empresa quando encontram oportunidade melhor.

## Capitalism Lab

Fontes:
- https://www.capitalismlab.com/improvements/
- https://www.capitalismlab.com/new-features/

Padrões estudados:
- IA concorrente;
- market share;
- branding corporativo;
- preço, publicidade e produto;
- macroeconomia e dinâmica urbana.

Aplicado na 1.2:
- market share por família de produto;
- awareness, fidelidade, qualidade e estratégia;
- rival direto e intensidade de competição;
- IA ajustando marketing e preço;
- confiança econômica da cidade.

## Cities: Skylines II

Fontes:
- https://www.paradoxinteractive.com/games/cities-skylines-ii/features/economy-production
- https://www.paradoxinteractive.com/games/cities-skylines-ii/features/citizen-simulation-lifepath

Padrões estudados:
- cidadãos como agentes econômicos;
- lifepath;
- trabalho, estudo, compras e sono;
- localização, transporte, custo e lucro influenciando empresas;
- empresas procurando trabalhadores e clientes.

Aplicado na 1.2:
- atividade horária dos cidadãos;
- educação, carreira, relações e mobilidade;
- proximidade de bairro influenciando escolha de fornecedor;
- bairros com índices de custo, social e segurança.

## The Sims 4 — Businesses & Hobbies

Fonte:
- https://www.ea.com/pt-br/games/the-sims/the-sims-4/news/businesses-and-hobbies-expansion-pack

Padrões estudados:
- vida pessoal conectada ao negócio;
- pequenos negócios;
- funcionários, salários, reputação e vantagens;
- identidade do negócio.

Aplicado na 1.2:
- vida pessoal, estudo, socialização, saúde e trabalho no mesmo relógio;
- marca e reputação ligadas à operação da empresa.

## Big Ambitions

Fonte:
- https://store.steampowered.com/app/1331550/Big_Ambitions/

Padrões estudados:
- evolução de vida para empreendedor;
- negócios dentro de uma cidade;
- gestão de tempo, recursos e patrimônio.

Aplicado na 1.2:
- trajetória funcionário → empreendedor;
- tempo horário;
- patrimônio pessoal separado do caixa empresarial;
- capital do fundador, aporte e dívida.

## Regra de implementação

Referências servem para identificar padrões de design. O METRÓPOLE mantém:
- código próprio;
- arte própria/procedural;
- nomes e identidade próprios;
- sistemas econômicos próprios;
- nenhuma dependência de conteúdo protegido dos jogos pesquisados.
