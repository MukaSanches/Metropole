extends Node2D

const W := 1280.0
const H := 720.0

var mode := "title"
var room := 0
var player := Vector2(280, 520)
var facing := 1
var speed := 250.0
var objective := "Conversar com Samuel."
var dialogue := []
var dialogue_i := 0
var interacted_samuel := false
var time_minutes := 18.0 * 60.0 + 31.0
var hint := ""
var hint_t := 0.0
var font: Font

func _ready() -> void:
    font = ThemeDB.fallback_font
    set_process(true)
    set_process_input(true)
    queue_redraw()

func _process(delta: float) -> void:
    if mode == "game":
        time_minutes += delta * 0.18
        _update_player(delta)
        if hint_t > 0.0:
            hint_t -= delta
        queue_redraw()

func _input(event: InputEvent) -> void:
    if event.is_action_pressed("ui_cancel"):
        if mode == "game":
            mode = "pause"
        elif mode == "pause":
            mode = "game"
        queue_redraw()
        return

    if mode == "title":
        if event.is_action_pressed("ui_accept") or (event is InputEventMouseButton and event.pressed):
            mode = "intro"
            dialogue = [
                ["JACQUELINE", "Terça-feira. 18:31. De novo."],
                ["JACQUELINE", "A luz entra pela janela exatamente do mesmo jeito. O rádio repete a mesma notícia."],
                ["JACQUELINE", "Só que hoje... alguma coisa está diferente."],
                ["OBJETIVO", "Fale com Samuel antes de buscar Theo na escola."]
            ]
            dialogue_i = 0
            queue_redraw()
    elif mode == "intro" or mode == "dialogue":
        if event.is_action_pressed("ui_accept") or (event is InputEventMouseButton and event.pressed):
            dialogue_i += 1
            if dialogue_i >= dialogue.size():
                mode = "game"
                dialogue = []
                dialogue_i = 0
            queue_redraw()
    elif mode == "pause":
        if event.is_action_pressed("ui_accept"):
            mode = "game"
            queue_redraw()
    elif mode == "game":
        if event.is_action_pressed("ui_accept"):
            _interact()

func _update_player(delta: float) -> void:
    var x := Input.get_axis("ui_left", "ui_right")
    if abs(x) > 0.05:
        facing = 1 if x > 0 else -1
    player.x += x * speed * delta
    player.x = clamp(player.x, 70.0, W - 70.0)

    if room == 0 and player.x > 1190:
        room = 1
        player.x = 95
        _flash("Rua Aurora — use as setas para explorar.")
    elif room == 1 and player.x < 75:
        room = 0
        player.x = 1160
    elif room == 1 and player.x > 1195:
        room = 2
        player.x = 95
        _flash("Salão Lúmen Beleza")
    elif room == 2 and player.x < 75:
        room = 1
        player.x = 1160

func _interact() -> void:
    if room == 0:
        if abs(player.x - 760) < 125:
            interacted_samuel = true
            objective = "Buscar Theo na escola. Saia pela porta à direita."
            _say([
                ["SAMUEL", "Já tô saindo pro trabalho. Você vai buscar o Theo, né?"],
                ["JACQUELINE", "Vou. Mas você não tá sentindo que esse dia... já aconteceu?"],
                ["SAMUEL", "Depois a gente conversa. Se acontecer qualquer coisa, me liga."],
                ["SISTEMA", "Objetivo atualizado: buscar Theo na escola."]
            ])
        elif abs(player.x - 1035) < 100:
            _say([["JACQUELINE", "A mochila do Theo. Ele esqueceu o chaveiro de bola de novo."]])
        else:
            _flash("Chegue perto de Samuel e pressione ENTER.")
    elif room == 1:
        if abs(player.x - 690) < 120:
            if interacted_samuel:
                objective = "Levar Theo para casa. Algo está estranho no céu."
                _say([
                    ["THEO", "Mãe! Eu fiz dois gols hoje!"],
                    ["JACQUELINE", "Eu sabia que você ia conseguir."],
                    ["THEO", "Mas... olha meu desenho. Eu sonhei com isso antes da aula."],
                    ["SISTEMA", "Theo mostra uma cidade sob um sol enorme. No canto, está escrito: 18:43."],
                    ["JACQUELINE", "Theo... que horas você desenhou isso?"],
                    ["THEO", "Amanhã."]
                ])
            else:
                _say([["JACQUELINE", "Preciso falar com Samuel antes de sair."]])
        elif abs(player.x - 1010) < 100:
            _say([["JACQUELINE", "O relógio da praça está parado em 18:43."]])
        else:
            _flash("Explore a rua. ENTER interage com pontos próximos.")
    elif room == 2:
        if abs(player.x - 790) < 120:
            _say([
                ["COLEGA", "Jacque, você voltou? Achei que já tinha ido buscar o Theo."],
                ["JACQUELINE", "Eu fui."],
                ["COLEGA", "Como assim? Você acabou de sair daqui."],
                ["SISTEMA", "Uma mensagem chega no celular. Remetente: Jacqueline."],
                ["MENSAGEM", "NÃO DEIXE DAR 18:43."]
            ])
        else:
            _flash("O salão ainda está aberto. Há alguém perto das cadeiras.")

func _say(lines: Array) -> void:
    dialogue = lines
    dialogue_i = 0
    mode = "dialogue"
    queue_redraw()

func _flash(text: String) -> void:
    hint = text
    hint_t = 2.5
    queue_redraw()

func _draw() -> void:
    if mode == "title":
        _draw_title()
        return

    if room == 0:
        _draw_apartment()
    elif room == 1:
        _draw_street()
    else:
        _draw_salon()

    _draw_player(player)
    _draw_hud()

    if mode == "intro" or mode == "dialogue":
        _draw_dialogue()
    elif mode == "pause":
        draw_rect(Rect2(0, 0, W, H), Color(0.02, 0.02, 0.04, 0.75))
        _center_text("PAUSADO", Vector2(W / 2, 310), 38, Color.WHITE)
        _center_text("ENTER ou ESC para continuar", Vector2(W / 2, 365), 18, Color(0.8, 0.82, 0.9))

    if hint_t > 0:
        _center_box(hint, 612)

func _draw_title() -> void:
    draw_rect(Rect2(0, 0, W, H), Color("11111c"))
    for i in range(18):
        var t := float(i) / 17.0
        var c := Color(0.08 + 0.20 * t, 0.07 + 0.10 * t, 0.14 + 0.12 * t)
        draw_rect(Rect2(0, i * 40, W, 42), c)
    draw_circle(Vector2(930, 205), 105, Color("ffb56b"))
    draw_circle(Vector2(930, 205), 78, Color("ffd59a"))
    for i in range(16):
        var bw := 55 + (i % 4) * 12
        var bh := 100 + ((i * 47) % 230)
        var x := i * 86.0 - 20
        draw_rect(Rect2(x, H - bh - 70, bw, bh), Color("242434"))
        for wy in range(0, bh - 25, 30):
            if (i + wy / 30) % 3 != 0:
                draw_rect(Rect2(x + 12, H - bh - 52 + wy, 9, 8), Color("f6c66e"))
    draw_rect(Rect2(0, 650, W, 70), Color("090911"))
    _center_text("LÚMEN", Vector2(W / 2, 225), 92, Color("fff4e6"))
    _center_text("O ÚLTIMO DIA CLARO", Vector2(W / 2, 292), 26, Color("ffb870"))
    _center_text("Algumas memórias nunca se apagam.", Vector2(W / 2, 340), 21, Color(0.87, 0.82, 0.82))
    _center_text("PROTÓTIPO JOGÁVEL • V0.1", Vector2(W / 2, 455), 17, Color(0.65, 0.67, 0.74))
    _center_text("Pressione ENTER para começar", Vector2(W / 2, 515), 22, Color.WHITE)
    _center_text("Setas: mover  •  ENTER: interagir  •  ESC: pausar", Vector2(W / 2, 565), 16, Color(0.72, 0.72, 0.8))

func _draw_apartment() -> void:
    draw_rect(Rect2(0, 0, W, H), Color("2a2026"))
    draw_rect(Rect2(0, 70, W, 495), Color("5c4744"))
    draw_rect(Rect2(0, 565, W, 155), Color("342728"))
    draw_rect(Rect2(75, 120, 330, 230), Color("261f31"))
    draw_rect(Rect2(88, 133, 304, 204), Color("d98167"))
    draw_circle(Vector2(330, 185), 45, Color("ffcc82"))
    for x in range(100, 385, 48):
        var h := 35 + x % 70
        draw_rect(Rect2(x, 337 - h, 35, h), Color("393340"))
    draw_rect(Rect2(235, 133, 8, 204), Color("2a2026"))
    draw_rect(Rect2(485, 390, 360, 125), Color("363341"))
    draw_rect(Rect2(505, 365, 320, 55), Color("454252"))
    draw_rect(Rect2(470, 410, 35, 95), Color("2f2d38"))
    draw_rect(Rect2(825, 410, 35, 95), Color("2f2d38"))
    draw_rect(Rect2(425, 520, 500, 35), Color("7a4f45"))
    draw_rect(Rect2(555, 490, 220, 35), Color("4b3430"))
    draw_rect(Rect2(1030, 430, 38, 105), Color("6b4936"))
    for p in [Vector2(1048, 410), Vector2(1018, 425), Vector2(1076, 426), Vector2(1030, 388), Vector2(1070, 395)]:
        draw_circle(p, 28, Color("40634e"))
    draw_rect(Rect2(1160, 245, 90, 320), Color("201b21"))
    draw_circle(Vector2(1180, 410), 6, Color("d6b269"))
    _text("SAÍDA", Vector2(1175, 230), 14, Color(0.9, 0.82, 0.72))
    _draw_samuel(Vector2(760, 520))
    _draw_marker(Vector2(760, 410), "Samuel")
    draw_rect(Rect2(1005, 500, 55, 50), Color("314862"))
    draw_circle(Vector2(1032, 495), 18, Color("314862"))
    _text("Mochila", Vector2(985, 472), 14, Color(0.85, 0.85, 0.9))

func _draw_street() -> void:
    for i in range(10):
        var t := float(i) / 9.0
        draw_rect(Rect2(0, i * 56, W, 58), Color(0.17 + 0.28 * t, 0.10 + 0.12 * t, 0.20 + 0.14 * t))
    draw_circle(Vector2(1040, 145), 95, Color("f6aa68"))
    for i in range(12):
        var bh := 120 + ((i * 61) % 170)
        draw_rect(Rect2(i * 112 - 20, 565 - bh, 95, bh), Color("34303f"))
        for y in range(565 - bh + 25, 540, 33):
            draw_rect(Rect2(i * 112 + 4, y, 10, 9), Color("e7b56c"))
    draw_rect(Rect2(0, 430, 310, 135), Color("293248"))
    draw_rect(Rect2(925, 420, 355, 145), Color("442c43"))
    _text("PADARIA AURORA", Vector2(55, 470), 20, Color("f3d6a1"))
    _text("LÚMEN BELEZA →", Vector2(990, 465), 21, Color("ffb0ca"))
    draw_rect(Rect2(0, 565, W, 155), Color("25242c"))
    draw_rect(Rect2(0, 565, W, 20), Color("aaa095"))
    for x in range(0, 1280, 120):
        draw_rect(Rect2(x + 20, 650, 65, 5), Color(0.65, 0.59, 0.47))
    draw_rect(Rect2(585, 390, 220, 175), Color("22313b"))
    for x in range(600, 790, 30):
        draw_rect(Rect2(x, 410, 5, 155), Color("8a9097"))
    _text("ESCOLA MUNICIPAL", Vector2(595, 375), 17, Color.WHITE)
    _draw_theo(Vector2(690, 525))
    _draw_marker(Vector2(690, 420), "Theo")
    draw_rect(Rect2(975, 345, 70, 220), Color("4b4140"))
    draw_circle(Vector2(1010, 345), 45, Color("e2ddd1"))
    _text("18:43", Vector2(984, 352), 13, Color("34282c"))
    _text("← CASA", Vector2(30, 605), 15, Color(0.85, 0.85, 0.9))
    _text("SALÃO →", Vector2(1170, 605), 15, Color(0.85, 0.85, 0.9))

func _draw_salon() -> void:
    draw_rect(Rect2(0, 0, W, H), Color("211b27"))
    draw_rect(Rect2(0, 80, W, 485), Color("51384d"))
    draw_rect(Rect2(0, 565, W, 155), Color("2c2330"))
    _text("LÚMEN", Vector2(520, 130), 55, Color("ffb1ce"))
    _text("BELEZA • TRANSFORMA • HISTÓRIAS", Vector2(442, 168), 16, Color("f7d6e2"))
    for x in [240, 560, 880]:
        draw_rect(Rect2(x, 215, 170, 195), Color("2f2936"))
        draw_rect(Rect2(x + 12, 227, 146, 150), Color("917282"))
        draw_rect(Rect2(x + 45, 430, 80, 70), Color("332b38"))
        draw_rect(Rect2(x + 32, 495, 105, 18), Color("24202a"))
    draw_rect(Rect2(480, 525, 320, 40), Color("573d45"))
    for x in range(505, 775, 45):
        draw_circle(Vector2(x, 515), 10, Color("d48ba7"))
    _draw_npc(Vector2(790, 520), Color("8e4f6d"), Color("d9b39f"))
    _draw_marker(Vector2(790, 410), "Colega")
    _text("← RUA", Vector2(30, 605), 15, Color(0.85, 0.85, 0.9))

func _draw_player(p: Vector2) -> void:
    _ellipse(p + Vector2(0, 18), Vector2(24, 7), Color(0, 0, 0, 0.3))
    draw_rect(Rect2(p.x - 13, p.y - 36, 10, 38), Color("273143"))
    draw_rect(Rect2(p.x + 3, p.y - 36, 10, 38), Color("273143"))
    draw_rect(Rect2(p.x - 17, p.y - 2, 15, 7), Color("18181d"))
    draw_rect(Rect2(p.x + 3, p.y - 2, 15, 7), Color("18181d"))
    draw_rect(Rect2(p.x - 24, p.y - 86, 48, 52), Color("272b34"))
    draw_rect(Rect2(p.x - 7, p.y - 83, 14, 45), Color("b05d6a"))
    draw_circle(Vector2(p.x, p.y - 107), 22, Color("c9937e"))
    draw_circle(Vector2(p.x, p.y - 117), 24, Color("211b22"))
    draw_rect(Rect2(p.x - 25, p.y - 118, 10, 40), Color("211b22"))
    draw_rect(Rect2(p.x + 16, p.y - 118, 10, 40), Color("211b22"))
    draw_circle(Vector2(p.x + facing * 7, p.y - 106), 2.5, Color("20191b"))

func _draw_samuel(p: Vector2) -> void:
    _ellipse(p + Vector2(0, 18), Vector2(22, 7), Color(0, 0, 0, 0.3))
    draw_rect(Rect2(p.x - 13, p.y - 38, 11, 40), Color("252d37"))
    draw_rect(Rect2(p.x + 3, p.y - 38, 11, 40), Color("252d37"))
    draw_rect(Rect2(p.x - 25, p.y - 91, 50, 55), Color("171b21"))
    draw_rect(Rect2(p.x - 23, p.y - 78, 46, 18), Color("27333b"))
    _text("SEG", Vector2(p.x - 14, p.y - 64), 9, Color.WHITE)
    draw_circle(Vector2(p.x, p.y - 112), 22, Color("b9826e"))
    draw_rect(Rect2(p.x - 20, p.y - 135, 40, 13), Color("211c1b"))

func _draw_theo(p: Vector2) -> void:
    _ellipse(p + Vector2(0, 16), Vector2(18, 6), Color(0, 0, 0, 0.3))
    draw_rect(Rect2(p.x - 10, p.y - 26, 8, 28), Color("273451"))
    draw_rect(Rect2(p.x + 2, p.y - 26, 8, 28), Color("273451"))
    draw_rect(Rect2(p.x - 20, p.y - 68, 40, 43), Color("d7d5c4"))
    draw_circle(Vector2(p.x, p.y - 86), 18, Color("d0a083"))
    draw_rect(Rect2(p.x - 17, p.y - 104, 34, 10), Color("4b352c"))
    draw_circle(Vector2(p.x + 32, p.y - 4), 16, Color("eee9da"))
    draw_arc(Vector2(p.x + 32, p.y - 4), 16, 0, TAU, 16, Color("24242a"), 2)

func _draw_npc(p: Vector2, clothes: Color, skin: Color) -> void:
    draw_rect(Rect2(p.x - 12, p.y - 34, 10, 36), Color("2d2b34"))
    draw_rect(Rect2(p.x + 3, p.y - 34, 10, 36), Color("2d2b34"))
    draw_rect(Rect2(p.x - 23, p.y - 80, 46, 48), clothes)
    draw_circle(Vector2(p.x, p.y - 99), 20, skin)
    draw_circle(Vector2(p.x, p.y - 108), 21, Color("3a292b"))

func _draw_hud() -> void:
    draw_rect(Rect2(22, 20, 390, 92), Color(0.06, 0.06, 0.09, 0.88))
    draw_rect(Rect2(22, 20, 5, 92), Color("d48680"))
    _text("OBJETIVO ATUAL", Vector2(43, 47), 14, Color("e1a492"))
    _text(objective, Vector2(43, 78), 17, Color.WHITE)
    var hour := int(time_minutes / 60.0) % 24
    var minute := int(time_minutes) % 60
    var ts := "%02d:%02d" % [hour, minute]
    draw_rect(Rect2(1060, 20, 195, 58), Color(0.06, 0.06, 0.09, 0.88))
    _text("TER  •  " + ts, Vector2(1080, 48), 20, Color.WHITE)
    _text("AURORA — SP", Vector2(1094, 69), 12, Color(0.75, 0.76, 0.82))
    draw_rect(Rect2(22, 660, 430, 38), Color(0.05, 0.05, 0.07, 0.76))
    _text("← → mover   ENTER interagir   ESC pausar", Vector2(38, 685), 15, Color(0.82, 0.82, 0.88))

func _draw_dialogue() -> void:
    if dialogue.is_empty() or dialogue_i >= dialogue.size():
        return
    draw_rect(Rect2(0, 0, W, H), Color(0, 0, 0, 0.16))
    draw_rect(Rect2(105, 485, 1070, 185), Color(0.045, 0.04, 0.055, 0.96))
    draw_rect(Rect2(105, 485, 7, 185), Color("d98982"))
    var who := str(dialogue[dialogue_i][0])
    var line := str(dialogue[dialogue_i][1])
    _text(who, Vector2(140, 525), 20, Color("efac97"))
    _wrapped(line, Vector2(140, 560), 980, 22, Color.WHITE)
    _text("ENTER  ›", Vector2(1055, 642), 15, Color(0.72, 0.72, 0.8))

func _draw_marker(p: Vector2, label: String) -> void:
    draw_circle(p, 10, Color("f1c678"))
    _center_text("!", p + Vector2(0, 5), 16, Color("30272a"))
    _center_text(label, p + Vector2(0, -20), 13, Color.WHITE)

func _center_box(s: String, y: float) -> void:
    var box_w := min(900.0, max(320.0, font.get_string_size(s, HORIZONTAL_ALIGNMENT_LEFT, -1, 17).x + 60.0))
    draw_rect(Rect2((W - box_w) / 2, y, box_w, 45), Color(0.04, 0.04, 0.06, 0.92))
    _center_text(s, Vector2(W / 2, y + 28), 17, Color.WHITE)

func _text(s: String, p: Vector2, size: int, color: Color) -> void:
    draw_string(font, p, s, HORIZONTAL_ALIGNMENT_LEFT, -1, size, color)

func _center_text(s: String, p: Vector2, size: int, color: Color) -> void:
    var sz := font.get_string_size(s, HORIZONTAL_ALIGNMENT_LEFT, -1, size)
    draw_string(font, Vector2(p.x - sz.x / 2, p.y), s, HORIZONTAL_ALIGNMENT_LEFT, -1, size, color)

func _wrapped(s: String, p: Vector2, width: float, size: int, color: Color) -> void:
    var words := s.split(" ")
    var line := ""
    var y := p.y
    for word in words:
        var test := line + (" " if line != "" else "") + word
        if font.get_string_size(test, HORIZONTAL_ALIGNMENT_LEFT, -1, size).x > width and line != "":
            _text(line, Vector2(p.x, y), size, color)
            y += size + 9
            line = word
        else:
            line = test
    if line != "":
        _text(line, Vector2(p.x, y), size, color)

func _ellipse(center: Vector2, radii: Vector2, color: Color) -> void:
    var pts := PackedVector2Array()
    for i in range(24):
        var a := TAU * float(i) / 24.0
        pts.append(center + Vector2(cos(a) * radii.x, sin(a) * radii.y))
    draw_colored_polygon(pts, color)
