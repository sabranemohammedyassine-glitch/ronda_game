"""Zellige UI kit sample + avatar. No text is baked into any asset.

Usage: python gen_ui.py [--out DIR]
Outputs: ui_btn_primary, ui_panel, ui_score_capsule, fx_card_select, ui_turn_ring,
         icon_settings, frame_avatar, avatar_01  (all .png, RGBA)
"""
import argparse
import math
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import zellige_common as Z
from zellige_common import PAL, rgb

SS = 4


def _canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def _u(v):
    return v * SS


def _rr_body(size, box, radius, top, bottom, border, bw, shadow=(8, 5, 120)):
    """Rounded body with vertical gradient, border and soft drop shadow. Units: final px."""
    w, h = size
    img = _canvas(w, h)
    hb = [_u(v) for v in box]
    m = Z.rr_mask(img.size, hb, _u(radius))
    if shadow:
        blur, dy, alpha = shadow
        img.alpha_composite(Z.shadow(m, _u(blur), (0, _u(dy)), "#02060F", alpha))
    if isinstance(top, tuple):       # (hex, alpha) flat/gradient with alpha
        (tc, ta), (bc, ba) = top, bottom
        fill = Z.vgrad(img.size, tc, bc).convert("RGBA")
        am = Image.linear_gradient("L").resize(img.size).point(lambda v: ta + (ba - ta) * v // 255)
        fill.putalpha(ImageChops.multiply(m, am))
    else:
        g = Z.vgrad((img.width, int(hb[3] - hb[1])), top, bottom)
        fill = Image.new("RGBA", img.size, (0, 0, 0, 0))
        fill.paste(g, (0, int(hb[1])))
        fill.putalpha(m)
    img.alpha_composite(fill)
    d = ImageDraw.Draw(img)
    if bw:
        d.rounded_rectangle(hb, radius=_u(radius), outline=rgb(border), width=int(_u(bw)))
    return img, d, hb


def btn_primary():
    # 240x112 canvas; body 216x84 at (12,8); radius 26. 9-slice L44 R44 T40 B48.
    img, d, hb = _rr_body((240, 112), [12, 8, 228, 92], 26, PAL["gold_hi"], "#C7972F", "#7E5C14", 2.5,
                          shadow=(7, 6, 140))
    # lower lip (depth) and top gloss, both inside the body
    body = Z.rr_mask(img.size, hb, _u(26))
    lip = Image.new("L", img.size, 0)
    ImageDraw.Draw(lip).rounded_rectangle([hb[0], hb[1] - _u(7), hb[2], hb[3] - _u(7)], radius=_u(26), fill=255)
    lipm = ImageChops.multiply(body, ImageChops.invert(lip))
    img.alpha_composite(Z.colorize(lipm, "#8E6A1A", 190))
    gloss = Image.new("L", img.size, 0)
    ImageDraw.Draw(gloss).rounded_rectangle([hb[0] + _u(6), hb[1] + _u(5), hb[2] - _u(6), hb[1] + _u(36)],
                                            radius=_u(20), fill=255)
    fade = Image.linear_gradient("L").resize(img.size).point(lambda v: max(0, 150 - v * 5))
    img.alpha_composite(Z.colorize(ImageChops.multiply(gloss, fade), "#FFF6D8", 150))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(hb, radius=_u(26), outline=rgb("#7E5C14"), width=int(_u(2.5)))
    d.rounded_rectangle([hb[0] + _u(5), hb[1] + _u(5), hb[2] - _u(5), hb[3] - _u(11)], radius=_u(21),
                        outline=rgb("#FBE7A8"), width=int(_u(1.2)))
    return Z.down(img, SS)


def panel():
    # 160x160; body inset 10, radius 24. 9-slice 48 all sides.
    img, d, hb = _rr_body((160, 160), [10, 8, 150, 148], 24, (PAL["navy_mid"], 232), (PAL["navy"], 240),
                          PAL["silver_mid"], 2, shadow=(8, 4, 130))
    d.rounded_rectangle([hb[0] + _u(6), hb[1] + _u(6), hb[2] - _u(6), hb[3] - _u(6)], radius=_u(18),
                        outline=rgb(PAL["silver"]) + (70,), width=int(_u(1)))
    return Z.down(img, SS)


def score_capsule():
    # 240x80; body 224x60 at (8,8) full-pill radius 30. 9-slice L44 R44 (height fixed).
    img, d, hb = _rr_body((240, 80), [8, 8, 232, 68], 30, (PAL["navy_deep"], 245), ("#0B1834", 245),
                          PAL["silver"], 2, shadow=(6, 4, 120))
    d.rounded_rectangle([hb[0] + _u(5), hb[1] + _u(5), hb[2] - _u(5), hb[3] - _u(5)], radius=_u(25),
                        outline=rgb(PAL["gold"]) + (200,), width=int(_u(1)))
    return Z.down(img, SS)


def card_select(cw=360, ch=540, pad=40):
    # glow sized for a 360x540 card + 40px margin -> 440x620. 9-slice 96 all sides.
    w, h = cw + 2 * pad, ch + 2 * pad
    ss = 2
    size = (w * ss, h * ss)
    box = [pad * ss, pad * ss, (pad + cw) * ss - 1, (pad + ch) * ss - 1]
    m = Z.rr_mask(size, box, 26 * ss)
    wide = Z.rr_mask(size, [box[0] - 6 * ss, box[1] - 6 * ss, box[2] + 6 * ss, box[3] + 6 * ss], 32 * ss)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    img.alpha_composite(Z.colorize(wide.filter(ImageFilter.GaussianBlur(13 * ss)), PAL["gold"], 215))
    img.alpha_composite(Z.colorize(wide.filter(ImageFilter.GaussianBlur(3 * ss)), PAL["gold_hi"], 255))
    # hollow centre (2 px under the card edge) so it never tints the card face
    inner = Z.rr_mask(size, [box[0] + 2 * ss, box[1] + 2 * ss, box[2] - 2 * ss, box[3] - 2 * ss], 24 * ss)
    img.putalpha(ImageChops.multiply(img.getchannel("A"), ImageChops.invert(inner)))
    return Z.down(img, ss)


def turn_ring(size=400):
    # orange/gold glow ring to sit behind/around frame_avatar (320). Rotate in Unity.
    img = _canvas(size, size)
    c = _u(size / 2)
    ring = Image.new("L", img.size, 0)
    rd = ImageDraw.Draw(ring)
    Z.circle(rd, c, c, _u(170), fill=255)
    Z.circle(rd, c, c, _u(152), fill=0)
    img.alpha_composite(Z.colorize(ring.filter(ImageFilter.GaussianBlur(_u(9))), PAL["orange"], 235))
    crisp = Image.new("L", img.size, 0)
    cd = ImageDraw.Draw(crisp)
    for i in range(8):                       # 8 arc segments with gaps
        a0 = i * 45 + 5
        cd.arc([c - _u(163), c - _u(163), c + _u(163), c + _u(163)], a0, a0 + 35, fill=255, width=int(_u(5)))
    img.alpha_composite(Z.colorize(crisp, PAL["orange_hi"]))
    d = ImageDraw.Draw(img)
    for i in range(8):                       # gold diamonds in the gaps
        a = math.radians(i * 45)
        d.polygon(Z.ngon(c + _u(160.5) * math.cos(a), c + _u(160.5) * math.sin(a), _u(6.5), 4, i * 45),
                  fill=rgb(PAL["gold_hi"]))
    return Z.down(img, SS)


def avatar_frame(size=320):
    # silver ring r 126..148, gold inner ring, opening r 124 (shows a 256 avatar at 1:1)
    img = _canvas(size, size)
    c = _u(size / 2)
    ring = Image.new("L", img.size, 0)
    rd = ImageDraw.Draw(ring)
    Z.circle(rd, c, c, _u(149), fill=255)
    Z.circle(rd, c, c, _u(124), fill=0)
    img.alpha_composite(Z.shadow(ring, _u(5), (0, _u(4)), "#02060F", 150))
    metal = Z.vgrad(img.size, PAL["silver_hi"], PAL["silver_mid"]).convert("RGBA")
    metal.putalpha(ring)
    img.alpha_composite(metal)
    d = ImageDraw.Draw(img)
    Z.circle(d, c, c, _u(149), outline=rgb(PAL["silver_dark"]), width=int(_u(2)))
    Z.circle(d, c, c, _u(141), outline=rgb("#FFFFFF") + (150,), width=int(_u(1.2)))
    Z.circle(d, c, c, _u(131), outline=rgb(PAL["silver_dark"]), width=int(_u(1.5)))
    Z.circle(d, c, c, _u(127.5), outline=rgb(PAL["gold"]), width=int(_u(4)))
    Z.circle(d, c, c, _u(124), outline=rgb(PAL["gold_dark"]), width=int(_u(1.2)))
    for i in range(8):                       # engraved stars on the ring; gold on the 4 axes
        a = math.radians(i * 45 + 22.5)
        x, y = c + _u(136.5) * math.cos(a), c + _u(136.5) * math.sin(a)
        d.polygon(Z.star8(x, y, _u(5)), fill=rgb(PAL["silver_dark"]))
    for i in range(4):
        a = math.radians(i * 90)
        x, y = c + _u(136.5) * math.cos(a), c + _u(136.5) * math.sin(a)
        d.polygon(Z.star8(x, y, _u(7.5)), fill=rgb(PAL["gold_dark"]))
        d.polygon(Z.star8(x, y, _u(5.5)), fill=rgb(PAL["gold_hi"]))
    return Z.down(img, SS)


def icon_settings(size=128):
    # pure white gear (tint in Unity via Image.color)
    img = _canvas(size, size)
    c = _u(size / 2)
    m = Image.new("L", img.size, 0)
    d = ImageDraw.Draw(m)
    teeth, ro, rr = 8, _u(56), _u(43)
    pts = []
    step = 2 * math.pi / teeth
    for i in range(teeth):
        a = i * step
        for da, r in [(-.27, rr), (-.17, ro), (.17, ro), (.27, rr)]:
            pts.append((c + r * math.cos(a + da * step * 2), c + r * math.sin(a + da * step * 2)))
    d.polygon(pts, fill=255)
    Z.circle(d, c, c, rr + _u(1), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(_u(1.6))).point(lambda v: 255 if v > 127 else 0)  # round the corners
    ImageDraw.Draw(m).ellipse([c - _u(19), c - _u(19), c + _u(19), c + _u(19)], fill=0)
    return Z.down(Z.colorize(m, "#FFFFFF"), SS)


AVATAR_01 = {"bg": ("#F3C98F", "#D98A48"), "halo": "#F9DDB0", "skin": "#C98F62", "skin_sh": "#B0774C",
             "hair": "#2B2024", "garment": PAL["royal"], "trim": PAL["gold"]}


def avatar_01(size=256, v=AVATAR_01):
    """Fictional flat/geometric bust. Square, opaque; mask to a circle in Unity.
    `v` is a colour-variant dict so further avatars are data, not new drawing code."""
    k = size / 256 * SS
    u = lambda x: x * k
    img = Z.vgrad((size * SS, size * SS), v["bg"][0], v["bg"][1]).convert("RGBA")
    d = ImageDraw.Draw(img)
    # quiet zellige halo
    Z.outline(d, Z.star8(u(128), u(122), u(118)), rgb(v["halo"]), u(3))
    Z.outline(d, Z.star8(u(128), u(122), u(118), 22.5), rgb(v["halo"]), u(3))
    skin, skin_sh, hair = rgb(v["skin"]), rgb(v["skin_sh"]), rgb(v["hair"])
    # shoulders + garment
    d.ellipse([u(18), u(190), u(238), u(350)], fill=rgb(v["garment"]))
    d.polygon([(u(92), u(196)), (u(128), u(250)), (u(164), u(196))], fill=rgb(PAL["ivory"]))
    d.line([(u(84), u(200)), (u(128), u(262)), (u(172), u(200))], fill=rgb(v["trim"]), width=int(u(5)), joint="curve")
    # neck
    d.rounded_rectangle([u(108), u(150), u(148), u(214)], radius=u(18), fill=skin_sh)
    # hair mass, ears, face
    d.ellipse([u(74), u(46), u(182), u(160)], fill=hair)
    d.ellipse([u(74), u(108), u(92), u(136)], fill=skin_sh)
    d.ellipse([u(164), u(108), u(182), u(136)], fill=skin_sh)
    d.ellipse([u(84), u(66), u(172), u(180)], fill=skin)
    # fringe: side-swept
    d.polygon([(u(x), u(y)) for x, y in Z.arc_pts(128, 96, 46, 38, 180, 360)] +
              [(u(170), u(104)), (u(150), u(92)), (u(118), u(100)), (u(98), u(112)), (u(86), u(108))], fill=hair)
    # features
    for ex in (110, 146):
        d.ellipse([u(ex - 5), u(124 - 6.5), u(ex + 5), u(124 + 6.5)], fill=hair)
        Z.circle(d, u(ex + 1.6), u(121.5), u(1.7), fill=rgb("#FFFFFF"))
    d.line([(u(100), u(110)), (u(119), u(108))], fill=hair, width=int(u(4)))
    d.line([(u(137), u(108)), (u(156), u(110))], fill=hair, width=int(u(4)))
    d.line([(u(128), u(128)), (u(124), u(144)), (u(131), u(145))], fill=skin_sh, width=int(u(3)), joint="curve")
    d.arc([u(110), u(134), u(146), u(166)], 25, 155, fill=rgb("#7A3F2E"), width=int(u(4)))
    return Z.down(img, SS).convert("RGB").convert("RGBA")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=Z.EXPORTED)
    a = ap.parse_args()
    for name, fn in [("ui_btn_primary", btn_primary), ("ui_panel", panel), ("ui_score_capsule", score_capsule),
                     ("fx_card_select", card_select), ("ui_turn_ring", turn_ring), ("icon_settings", icon_settings),
                     ("frame_avatar", avatar_frame), ("avatar_01", avatar_01)]:
        Z.save(fn(), name + ".png", a.out)
