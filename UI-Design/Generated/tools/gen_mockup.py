"""Review images (NOT game assets) written to UI-Design/Generated/:
  mockup_game_screen.png   1920x1080 composite of the sample assets in the main-screen layout
  contact_sheet_140.png    sample cards at 140 px and 120 px wide on the table background
  deck_preview_all40.png   all 40 faces rendered in memory (proves the generator; nothing exported)

Usage: python gen_mockup.py     (run gen_background.py, gen_cards.py --sample, gen_ui.py first)
Placeholder dynamic text appears here only; it is never baked into exported assets.
"""
import os
from PIL import Image, ImageDraw, ImageFilter
import zellige_common as Z
from zellige_common import PAL, rgb
import gen_cards
import gen_ui

E, G = Z.EXPORTED, Z.GENERATED
F_DISPLAY = os.path.join(Z.FONT_DIR, "cambriab.ttf")


def load(name):
    p = os.path.join(E, "cards", name + ".png")
    if not (name.startswith("card_") and os.path.exists(p)):
        p = os.path.join(E, name + ".png")
    return Image.open(p).convert("RGBA")


def scaled(img, w):
    return img.resize((int(w), int(round(img.height * w / img.width))), Image.LANCZOS)


def nine_slice(img, L, T, R, B, w, h):
    """Stretch a sprite the way Unity's Image (Sliced) does."""
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    sx = [0, L, img.width - R, img.width]
    sy = [0, T, img.height - B, img.height]
    dx = [0, L, w - R, w]
    dy = [0, T, h - B, h]
    for j in range(3):
        for i in range(3):
            part = img.crop((sx[i], sy[j], sx[i + 1], sy[j + 1]))
            tw, th = dx[i + 1] - dx[i], dy[j + 1] - dy[j]
            if tw > 0 and th > 0:
                out.paste(part.resize((tw, th), Image.BILINEAR), (dx[i], dy[j]))
    return out


def drop(canvas, img, cx, cy, angle=0, shadow=True, blur=10, dy=8, alpha=120):
    """Composite img centred at (cx, cy), optionally rotated, with a soft contact shadow."""
    if angle:
        img = img.rotate(angle, Image.BICUBIC, expand=True)
    x, y = int(round(cx - img.width / 2)), int(round(cy - img.height / 2))
    if shadow:
        pad = blur * 3
        m = Image.new("L", (img.width + 2 * pad, img.height + 2 * pad), 0)
        m.paste(img.getchannel("A"), (pad, pad))
        sh = Z.colorize(m.filter(ImageFilter.GaussianBlur(blur)), "#01040C", alpha)
        canvas.alpha_composite(sh, (max(0, x - pad), max(0, y - pad + dy)),
                               (max(0, pad - x), max(0, pad - dy - y), sh.width, sh.height))
    canvas.alpha_composite(img, (max(0, x), max(0, y)), (max(0, -x), max(0, -y), img.width, img.height))


def text(d, xy, s, font, fill, anchor="la", spacing=0):
    if spacing:
        x, y = xy
        total = sum(d.textlength(ch, font=font) + spacing for ch in s) - spacing
        if anchor[0] == "m":
            x -= total / 2
        elif anchor[0] == "r":
            x -= total
        for ch in s:
            d.text((x, y), ch, font=font, fill=fill, anchor="l" + anchor[1])
            x += d.textlength(ch, font=font) + spacing
    else:
        d.text(xy, s, font=font, fill=fill, anchor=anchor)


def avatar_badge(av, frame, diameter, ring=None):
    """Circular avatar under the frame (and optional turn ring). Returns RGBA square."""
    size = 400
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    if ring is not None:
        out.alpha_composite(ring)
    a = av.resize((256, 256), Image.LANCZOS)
    m = Image.new("L", (256 * 4, 256 * 4), 0)
    ImageDraw.Draw(m).ellipse([8, 8, 256 * 4 - 9, 256 * 4 - 9], fill=255)
    a.putalpha(m.resize((256, 256), Image.LANCZOS))
    out.alpha_composite(a, (72, 72))
    out.alpha_composite(frame, (40, 40))
    return scaled(out, diameter * 400 / 298)


def capsule(cap, w, scale=0.8):
    return scaled(nine_slice(cap, 44, 0, 44, 0, int(w / scale), cap.height), w)


def mockup():
    bg = load("bg_table_main")
    cv = bg.copy()
    d = ImageDraw.Draw(cv)
    card = gen_cards.Card()
    faces = {k: load(k) for k in ["card_oros_01", "card_copas_05", "card_espadas_07", "card_bastos_04",
                                  "card_oros_10", "card_espadas_12"]}
    extra = {k: card.face(*k) for k in [("bastos", 11), ("copas", 3), ("bastos", 6)]}   # in-memory only
    back = load("card_back")
    panel, cap, btn = load("ui_panel"), load("ui_score_capsule"), load("ui_btn_primary")
    frame, ring, gear, glow = load("frame_avatar"), load("ui_turn_ring"), load("icon_settings"), load("fx_card_select")
    av1 = load("avatar_01")
    av2 = load("avatar_02")
    f_name = Z.font(F_DISPLAY, 34)
    f_num = Z.font(F_DISPLAY, 34)
    f_lab = Z.font(Z.FONT_MOCK_UI, 19)
    f_small = Z.font(Z.FONT_MOCK_UI_REG, 20)

    def player_panel(x, y, av, name, sub, score, turn=False):
        cv.alpha_composite(nine_slice(panel, 48, 48, 48, 48, 448, 156), (x, y))
        badge = avatar_badge(av, frame, 104, ring if turn else None)
        cv.alpha_composite(badge, (int(x + 82 - badge.width / 2), int(y + 76 - badge.height / 2)))
        text(d, (x + 152, y + 36), name, f_name, rgb(PAL["text"]))
        text(d, (x + 153, y + 82), sub.upper(), f_lab, rgb(PAL["orange_hi"] if turn else PAL["text_2"]), spacing=2)
        c = capsule(cap, 120)
        cv.alpha_composite(c, (x + 300, y + 46))
        text(d, (x + 300 + c.width / 2, y + 46 + c.height / 2 - 2), str(score), f_num, rgb(PAL["gold_hi"]), "mm")

    # ---- top: opponent panel + opponent hand + system buttons
    player_panel(88, 80, av2, "Atlas", "Computer", 9)
    ob = scaled(back, 112)
    for i, (dx, ang, dyy) in enumerate([(-135, 8, 6), (-45, 3, 0), (45, -3, 0), (135, -8, 6)]):
        drop(cv, ob, 960 + dx * 0.9, 172 - dyy, ang)
    for i, x in enumerate([1708, 1788]):
        Z.circle(d, x, 128, 34, fill=rgb(PAL["navy"]), outline=rgb(PAL["silver_mid"]), width=2)
        Z.circle(d, x, 128, 28, outline=rgb(PAL["silver"]) + (70,), width=1)
    g = scaled(gear, 36)
    tint = Image.new("RGBA", g.size, rgb(PAL["silver_hi"]) + (0,))
    tint.putalpha(g.getchannel("A"))
    cv.alpha_composite(tint, (1788 - 18, 128 - 18))
    so = scaled(load("icon_sound_on"), 36)
    t2 = Image.new("RGBA", so.size, rgb(PAL["silver_hi"]) + (0,))
    t2.putalpha(so.getchannel("A"))
    cv.alpha_composite(t2, (1708 - 18, 128 - 18))

    # ---- right: scoreboard
    px, py = 1488, 214
    cv.alpha_composite(nine_slice(panel, 48, 48, 48, 48, 352, 250), (px, py))
    text(d, (px + 176, py + 40), "SCORE", f_lab, rgb(PAL["text_2"]), "mm", spacing=5)
    d.line([(px + 44, py + 62), (px + 308, py + 62)], fill=rgb(PAL["silver_mid"]) + (120,), width=1)
    d.polygon(Z.ngon(px + 176, py + 62, 6, 4), fill=rgb(PAL["gold"]))
    for i, (nm, sc) in enumerate([("You", 14), ("Atlas", 9)]):
        yy = py + 100 + i * 62
        text(d, (px + 44, yy), nm, f_name, rgb(PAL["text"]), "lm")
        c = capsule(cap, 112)
        cv.alpha_composite(c, (px + 204, int(yy - c.height / 2) + 2))
        text(d, (px + 204 + c.width / 2, yy), str(sc), f_num, rgb(PAL["gold_hi"]), "mm")
    text(d, (px + 176, py + 214), "First to 41", f_small, rgb(PAL["text_2"]), "mm")

    # ---- left: deck + counter
    db = scaled(back, 140)
    for k in range(3):
        drop(cv, db, 300 - 6 + k * 3, 520 - 6 + k * 3, 0, shadow=(k == 0), blur=12, dy=10)
    c = capsule(cap, 128)
    cv.alpha_composite(c, (300 - c.width // 2, 650))
    text(d, (300, 650 + c.height / 2 - 2), "24", f_num, rgb(PAL["text"]), "mm")
    text(d, (300, 728), "DECK", f_lab, rgb(PAL["text_2"]), "mm", spacing=4)

    # ---- centre: table cards (140 px wide) + call-out zone
    table = [faces["card_bastos_04"], faces["card_espadas_07"], faces["card_oros_10"], faces["card_copas_05"],
             extra[("bastos", 11)]]
    for i, (im, ang) in enumerate(zip(table, [2.5, -1.5, 1, -3, 2])):
        drop(cv, scaled(im, 140), 960 + (i - 2) * 166, 548 + (6 if i % 2 else -6), ang)
    f_call = Z.font(F_DISPLAY, 82)
    lay = Image.new("RGBA", cv.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(lay)
    ld.text((960, 338), "RONDA!", font=f_call, fill=rgb(PAL["gold_hi"]), anchor="mm",
            stroke_width=3, stroke_fill=rgb("#5E4510"))
    sh = Z.colorize(lay.getchannel("A").filter(ImageFilter.GaussianBlur(10)), "#01040C", 170)
    cv.alpha_composite(sh, (0, 6))
    cv.alpha_composite(lay)
    d = ImageDraw.Draw(cv)
    for sx in (760, 1160):
        d.polygon(Z.star8(sx, 338, 15), fill=rgb(PAL["gold"]))
        d.polygon(Z.star8(sx, 338, 8, 22.5), fill=rgb(PAL["navy"]))

    # ---- bottom: human hand (fan of 4, one selected), panel, primary button
    hand = [faces["card_oros_01"], extra[("copas", 3)], faces["card_espadas_12"], extra[("bastos", 6)]]
    for i, (im, ang) in enumerate(zip(hand, [7, 2.5, -2.5, -7])):
        x = 960 + (i - 1.5) * 190
        y = 858 + abs(i - 1.5) * 12
        if i == 2:      # selected: lifted + gold glow
            gl = scaled(glow, 176 * 440 / 360)
            drop(cv, gl, x, y - 40, ang, shadow=False)
            drop(cv, scaled(im, 176), x, y - 40, ang, blur=16, dy=18, alpha=110)
        else:
            drop(cv, scaled(im, 176), x, y, ang)
    d = ImageDraw.Draw(cv)
    player_panel(88, 844, av1, "You", "Your turn", 14, turn=True)
    drop(cv, scaled(load("ui_dealer_chip"), 48), 88 + 448 - 30, 844 + 4, blur=4, dy=3)
    b = nine_slice(btn, 44, 40, 44, 48, 248, 104)
    cv.alpha_composite(b, (1584, 872))
    text(d, (1584 + 124, 872 + 49), "MENU", Z.font(F_DISPLAY, 32), rgb(PAL["ink"]), "mm", spacing=3)
    Z.save(cv.convert("RGB"), "mockup_game_screen.png", G)


def contact_sheet():
    bg = load("bg_table_main").crop((320, 190, 320 + 1280, 190 + 640))
    names = ["card_oros_01", "card_copas_05", "card_espadas_07", "card_bastos_04", "card_oros_10",
             "card_espadas_12", "card_back"]
    for row, (w, y) in enumerate([(140, 185), (120, 470)]):
        gap = w + 28
        for i, n in enumerate(names):
            drop(bg, scaled(load(n), w), 640 + (i - 3) * gap, y)
    Z.save(bg.convert("RGB"), "contact_sheet_140.png", G)


def deck_preview():
    card = gen_cards.Card()
    w = 150
    sheet = Image.new("RGBA", (10 * (w + 14) + 14, 4 * (w * 3 // 2 + 14) + 14), rgb(PAL["navy_mid"]) + (255,))
    for j, s in enumerate(Z.SUIT_ORDER):
        for i, r in enumerate(Z.RANKS):
            sheet.alpha_composite(scaled(card.face(s, r), w), (14 + i * (w + 14), 14 + j * (w * 3 // 2 + 14)))
    Z.save(sheet.convert("RGB"), "deck_preview_all40.png", G)


if __name__ == "__main__":
    mockup()
    contact_sheet()
    deck_preview()
