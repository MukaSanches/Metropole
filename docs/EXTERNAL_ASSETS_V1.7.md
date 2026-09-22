# External Assets — METRÓPOLE ∞ 1.7 Visual Leap

## Política

A release só incorpora assets com licença redistribuível confirmada e origem pinada. Falha de download, dependência ausente ou importação inválida deve falhar no pipeline.

## Kenney — CC0

Packs utilizados/expandidos:
- City Kit (Commercial);
- City Kit (Industrial);
- City Kit (Roads);
- Car Kit;
- Factory Kit;
- Mini Characters;
- Interface Sounds.

A 1.7 amplia significativamente edifícios, veículos e props industriais. A aquisição permanece pinada por commit e gera manifest SHA-256.

## KayKit — CC0

Pack oficial: KayKit City Builder Bits 1.0.

Commit pinado:
`63976910ca04d16f0fc531b9c614244be8128713`

Uso:
- 8 edifícios residenciais;
- bancos;
- arbustos;
- lixeiras;
- hidrantes;
- postes;
- semáforos;
- caixa d'água e pequenos props urbanos.

GLTF, BIN e texture atlas são mantidos juntos para preservar as dependências do formato.

## Poly Haven — CC0

Retidos:
- Urban Street 02 HDRI;
- Asphalt 02 PBR;
- Concrete Pavement PBR.

## OpenGameArt — CC0

Retidos:
- ambiência urbana;
- chuva.

## Animações

O runtime cataloga todos os clips válidos presentes nos personagens efetivamente importados e escolhe animações conforme a atividade do cidadão. Clips incompatíveis não são forçados.

Foram avaliadas bibliotecas CC0 muito maiores, incluindo KayKit Character Animations e Quaternius Universal Animation Library. Elas exigem uma etapa própria de retargeting/rig unificado para serem usadas com segurança; portanto, não são declaradas integradas nesta release sem validação de deformação e skeleton mapping.

## Validação obrigatória

- menu 3D com assets reais;
- personagem animado no menu;
- camada premium com pelo menos 120 assets detalhados;
- pelo menos 30 props;
- AnimationPlayer real e clips catalogados;
- fallback leve funcional;
- export Windows;
- instalador;
- instalação limpa;
- execução pós-instalação.
