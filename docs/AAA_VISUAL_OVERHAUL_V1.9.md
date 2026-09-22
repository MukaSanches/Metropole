# METRÓPOLE ∞ — AAA Visual Overhaul (1.9)

Este incremento transforma a referência visual aprovada em rendering real do jogo. Nenhuma imagem pré-renderizada é usada como mapa: a cidade continua sendo executada em tempo real pelo Godot.

## Direção visual

- câmera premium em perspectiva com enquadramento aéreo cinematográfico;
- Forward+ em Ultra por padrão quando suportado, mantendo adaptação automática por frame time;
- TAA + MSAA + screen-space AA por perfil;
- HDRI/PBR, AGX, SSAO, SSIL, SSR, glow e fog preservados/adaptativos;
- contraste e saturação ajustados para uma leitura mais cinematográfica.

## Cidade

- fachadas procedurais PBR com janelas, metal/vidro e emissão noturna;
- variação de cor e vidro por distrito/riqueza;
- skyline com prédios mais altos e volumes de cobertura;
- telhados com volumes técnicos para quebrar o aspecto de “caixas”;
- árvores em copas compostas em vez de uma esfera simples;
- ruas mais largas, bordas de faixa, cruzamentos e 300 faixas de pedestre instanciadas;
- postes/luminárias com geometria e luz real;
- asfalto e pavimento continuam reagindo à chuva.

## Assets detalhados

A camada CC0 existente foi adensada sem mudar a verdade lógica da simulação:

- até 7 edifícios detalhados por distrito;
- mais mobiliário urbano;
- até 64 veículos detalhados;
- até 64 cidadãos detalhados e animados;
- os proxies continuam separados da simulação lógica;
- perfis inferiores desligam/reduzem detalhes antes de afetar mecânicas.

## Performance

A regra continua sendo **simular muito e renderizar apenas o necessário**.

- MultiMesh permanece responsável pela massa urbana;
- assets GLB/GLTF continuam limitados por orçamento;
- Auto pode reduzir Ultra → High → Medium → Low conforme frame time;
- fallback 2D permanece disponível por `METROPOLE_FORCE_FALLBACK=1`;
- a CI continua compilando, exportando, validando premium/fallback e gerando o instalador.

## Critério visual

A referência é uma cidade densa, moderna e legível, com sensação de profundidade, materiais convincentes, iluminação urbana e vida de rua — sem substituir o gameplay por uma imagem estática.
