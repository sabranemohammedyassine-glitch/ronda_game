"""Zellige UI kit, batch 2. No text is baked into any asset.

Usage: python gen_ui_extra.py [--out DIR]
Outputs: avatar_02, ui_card_slot, ui_dealer_chip, icon_sound_on, icon_sound_off, icon_restart,
         ui_btn_secondary, ui_dialog, ui_callout, illus_victory, illus_defeat (all .png, RGBA)
"""
import argparse
import math
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import zellige_common as Z
from zellige_common import PAL, rgb
from gen_ui import SS, _canvas, _u, _rr_body

GOLD_INK = "#5E4510"


# ------------------------------------------------------------------ buttons / panels
def btn_secondary():
    # same geometry as ui_btn_primary: 240x112, body 216x84 at (12,8), r26. 9-slice L44 R44 T40 B48.
    img, d, hb = _rr_body((240, 112), [12, 8, 228, 92], 26, PAL["royal"], PAL["navy_mid"], PAL["silver"], 2.5,
                          shadow=(7, 6, 140))
    body = Z.rr_mask(img.size, hb, _u(26))
    lip = Image.new("L", img.size, 0)
    ImageDraw.Draw(lip).rounded_rectangle([hb[0], hb[1] - _u(7), hb[2], hb[3] - _u(7)], radius=_u(26), fill=255)
    img.alpha_composite(Z.colorize(ImageChops.multiply(body, ImageChops.invert(lip)), PAL["navy_deep"], 200))
    gloss = Image.new("L", img.size, 0)
    ImageDraw.Draw(gloss).rounded_rectangle([hb[0] + _u(6), hb[1] + _u(5), hb[2] - _u(6), hb[1] + _u(36)],
                                            radius=_u(20), fill=255)
    fade = Image.linear_gradient("L").resize(img.size).point(lambda v: max(0, 110 - v * 4))
    img.alpha_composite(Z.colorize(ImageChops.multiply(gloss, fade), PAL["silver_hi"], 110))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(hb, radius=_u(26), outline=rgb(PAL["silver"]), width=int(_u(2.5)))
    d.rounded_rectangle([hb[0] + _u(5), hb[1] + _u(5), hb[2] - _u(5), hb[3] - _u(11)], radius=_u(21),
                        outline=rgb(PAL["silver_mid"]) + (150,), width=int(_u(1.2)))
    return Z.down(img, SS)


def dialog():
    # 256x256; body (12,10)-(244,244) r28. 9-slice 72 all sides (corner ornaments stay inside).
    img, d, hb = _rr_body((256, 256), [12, 10, 244, 244], 28, (PAL["navy_mid"], 246), (PAL["navy"], 250),
                          PAL["silver"], 2.5, shadow=(10, 6, 150))
    d.rounded_rectangle([hb[0] + _u(8), hb[1] + _u(8), hb[2] - _u(8), hb[3] - _u(8)], radius=_u(21),
                        outline=rgb(PAL["silver_mid"]), width=int(_u(1.2)))
    d.rounded_rectangle([hb[0] + _u(12), hb[1] + _u(12), hb[2] - _u(12), hb[3] - _u(12)], radius=_u(17),
                        outline=rgb(PAL["gold"]) + (170,), width=int(_u(1)))
    for (x, y) in [(36, 34), (220, 34), (36, 220), (220, 220)]:     # gold corner stars
        d.polygon(Z.star8(_u(x), _u(y), _u(10)), fill=rgb(PAL["gold"]))
        d.polygon(Z.star8(_u(x), _u(y), _u(4.5), 22.5), fill=rgb(PAL["navy"]))
    return Z.down(img, SS)


def card_slot():
    # card-sized empty slot 360x540, same r26 as the cards. Simple mode at card size; 9-slice 40 all.
    w, h = 360, 540
    img = _canvas(w, h)
    d = ImageDraw.Draw(img)
    box = [_u(2), _u(2), _u(w - 2), _u(h - 2)]
    d.rounded_rectangle(box, radius=_u(26), fill=rgb(PAL["navy_deep"]) + (46,))
    d.rounded_rectangle(box, radius=_u(26), outline=rgb(PAL["silver"]) + (70,), width=int(_u(3)))
    d.rounded_rectangle([_u(14), _u(14), _u(w - 14), _u(h - 14)], radius=_u(15),
                        outline=rgb(PAL["silver"]) + (34,), width=int(_u(2)))
    return Z.down(img, SS)


def callout():
    # 520x140 ribbon banner, notched ends with gold stars. 9-slice L110 R110 T0 B0 (stretch width only).
    w, h = 520, 140
    img = _canvas(w, h)
    cy = h / 2
    shape = [(18, 30), (w - 18, 30), (w - 48, cy), (w - 18, h - 30), (18, h - 30), (48, cy)]
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).polygon([(_u(x), _u(y)) for x, y in shape], fill=255)
    img.alpha_composite(Z.shadow(m, _u(8), (0, _u(5)), "#01040C", 150))
    fill = Z.vgrad(img.size, PAL["navy_mid"], PAL["navy_deep"]).convert("RGBA")
    fill.putalpha(m)
    img.alpha_composite(fill)
    d = ImageDraw.Draw(img)
    Z.outline(d, [(_u(x), _u(y)) for x, y in shape], rgb(PAL["silver"]), _u(2.5))
    inner = [(30, 38), (w - 30, 38), (w - 56, cy), (w - 30, h - 38), (30, h - 38), (56, cy)]
    Z.outline(d, [(_u(x), _u(y)) for x, y in inner], rgb(PAL["gold"]) + (190,), _u(1.2))
    for x in (84, w - 84):
        d.polygon(Z.star8(_u(x), _u(cy), _u(15)), fill=rgb(PAL["gold"]))
        d.polygon(Z.star8(_u(x), _u(cy), _u(7), 22.5), fill=rgb(PAL["navy_deep"]))
    return Z.down(img, SS)


def dealer_chip(size=64):
    img = _canvas(size, size)
    c = _u(size / 2)
    m = Image.new("L", img.size, 0)
    Z.circle(ImageDraw.Draw(m), c, c, _u(28), fill=255)
    img.alpha_composite(Z.shadow(m, _u(2.5), (0, _u(2)), "#01040C", 150))
    metal = Z.vgrad(img.size, PAL["gold_hi"], PAL["gold_dark"]).convert("RGBA")
    metal.putalpha(m)
    img.alpha_composite(metal)
    d = ImageDraw.Draw(img)
    Z.circle(d, c, c, _u(28), outline=rgb(GOLD_INK), width=int(_u(1.5)))
    Z.circle(d, c, c, _u(22), outline=rgb(GOLD_INK) + (160,), width=int(_u(1)))
    for i in range(16):        # milled edge notches
        a = math.radians(i * 22.5)
        Z.circle(d, c + _u(25) * math.cos(a), c + _u(25) * math.sin(a), _u(1.2), fill=rgb(PAL["gold_hi"]))
    d.polygon(Z.star8(c, c, _u(15)), fill=rgb(GOLD_INK))
    d.polygon(Z.star8(c, c, _u(12.5)), fill=rgb(PAL["gold"]))
    d.polygon(Z.star8(c, c, _u(6), 22.5), fill=rgb(PAL["gold_hi"]))
    return Z.down(img, SS)


# ------------------------------------------------------------------ icons (white, tint in Unity)
def _speaker(d, c):
    u = _u
    d.polygon([(c - u(42), c - u(16)), (c - u(20), c - u(16)), (c + u(6), c - u(40)), (c + u(6), c + u(40)),
               (c - u(20), c + u(16)), (c - u(42), c + u(16))], fill=255)


def _round(mask):
    return mask.filter(ImageFilter.GaussianBlur(_u(1.2))).point(lambda v: 255 if v > 127 else 0)


def icon_sound_on(size=128):
    m = Image.new("L", (_u(size), _u(size)), 0)
    d = ImageDraw.Draw(m)
    c = _u(size / 2)
    _speaker(d, c)
    m = _round(m)
    d = ImageDraw.Draw(m)
    for r in (22, 40):
        d.arc([c + _u(2) - _u(r), c - _u(r), c + _u(2) + _u(r), c + _u(r)], -48, 48, fill=255, width=int(_u(8)))
    return Z.down(Z.colorize(m, "#FFFFFF"), SS)


def icon_sound_off(size=128):
    m = Image.new("L", (_u(size), _u(size)), 0)
    d = ImageDraw.Draw(m)
    c = _u(size / 2)
    _speaker(d, c)
    m = _round(m)
    d = ImageDraw.Draw(m)
    x0, x1, y0, y1 = c + _u(20), c + _u(48), c - _u(14), c + _u(14)
    d.line([(x0, y0), (x1, y1)], fill=255, width=int(_u(8)))
    d.line([(x0, y1), (x1, y0)], fill=255, width=int(_u(8)))
    for (x, y) in [(x0, y0), (x1, y1), (x0, y1), (x1, y0)]:
        Z.circle(d, x, y, _u(4), fill=255)
    return Z.down(Z.colorize(m, "#FFFFFF"), SS)


def icon_restart(size=128):
    m = Image.new("L", (_u(size), _u(size)), 0)
    d = ImageDraw.Draw(m)
    c = _u(size / 2)
    r = _u(38)
    d.arc([c - r, c - r, c + r, c + r], -60, 225, fill=255, width=int(_u(10)))
    a = math.radians(-60)                                   # arrow head at the arc's open end
    ex, ey = c + (r - _u(5)) * math.cos(a), c + (r - _u(5)) * math.sin(a)
    tx, ty = math.cos(a + math.pi / 2), math.sin(a + math.pi / 2)   # tangent (direction of travel reversed)
    nx, ny = math.cos(a), math.sin(a)
    tip = (ex - tx * _u(22), ey - ty * _u(22))
    d.polygon([tip, (ex + nx * _u(17), ey + ny * _u(17)), (ex - nx * _u(17), ey - ny * _u(17))], fill=255)
    a2 = math.radians(225)
    Z.circle(d, c + (r - _u(5)) * math.cos(a2), c + (r - _u(5)) * math.sin(a2), _u(5), fill=255)
    return Z.down(Z.colorize(_round(m), "#FFFFFF"), SS)


# ------------------------------------------------------------------ avatar 02
def avatar_02(size=256):
    """Second fictional character (opponent): curly-haired figure with round glasses and a
    high collar; teal background. Different silhouette from avatar_01, not a recolour."""
    k = size / 256 * SS
    u = lambda x: x * k
    img = Z.vgrad((size * SS, size * SS), "#7FC4C0", "#2D7480").convert("RGBA")
    d = ImageDraw.Draw(img)
    for r in (120, 84):
        d.polygon(Z.star8(u(128), u(118), u(r), 22.5), outline=None)
        Z.outline(d, Z.star8(u(128), u(118), u(r), 22.5), rgb("#A9DCD6"), u(2.5))
    skin, skin_sh, hair = rgb("#8D5A3B"), rgb("#76492F"), rgb("#1E1A1C")
    # shoulders + high-collar garment
    d.ellipse([u(14), u(192), u(242), u(352)], fill=rgb("#C2552B"))
    d.rounded_rectangle([u(96), u(178), u(160), u(222)], radius=u(16), fill=rgb("#E07A3F"))
    for x in (110, 128, 146):
        Z.circle(d, u(x), u(238), u(3.5), fill=rgb(PAL["gold_hi"]))
    d.rounded_rectangle([u(112), u(156), u(144), u(196)], radius=u(14), fill=skin_sh)
    # curly hair: ring of circles, then face on top
    for i in range(14):
        a = math.radians(180 + i * (180 / 13))
        Z.circle(d, u(128 + 58 * math.cos(a)), u(108 + 52 * math.sin(a)), u(24), fill=hair)
    for (x, y) in [(72, 118), (184, 118), (76, 140), (180, 140)]:
        Z.circle(d, u(x), u(y), u(18), fill=hair)
    d.ellipse([u(86), u(70), u(170), u(184)], fill=skin)
    # short curls on the forehead
    for x in (100, 116, 132, 148, 160):
        Z.circle(d, u(x), u(78 + (4 if x in (100, 160) else 0)), u(12), fill=hair)
    # round glasses
    for ex in (110, 146):
        Z.circle(d, u(ex), u(126), u(14), fill=rgb("#A07257"))
        Z.circle(d, u(ex), u(126), u(14), outline=rgb(PAL["gold"]), width=int(u(3.5)))
        d.ellipse([u(ex - 4), u(126 - 5), u(ex + 4), u(126 + 5)], fill=hair)
    d.line([(u(124), u(124)), (u(132), u(124))], fill=rgb(PAL["gold"]), width=int(u(3)))
    d.line([(u(103), u(104)), (u(118), u(101))], fill=hair, width=int(u(4)))
    d.line([(u(138), u(101)), (u(153), u(104))], fill=hair, width=int(u(4)))
    d.line([(u(128), u(136)), (u(123), u(152)), (u(131), u(153))], fill=skin_sh, width=int(u(3)), joint="curve")
    d.arc([u(114), u(146), u(142), u(168)], 30, 150, fill=rgb("#4E2A1E"), width=int(u(4)))
    return Z.down(img, SS)


# ------------------------------------------------------------------ result illustrations
def _rays(img, cx, cy, n, r0, r1, color, alpha, wd):
    m = Image.new("L", img.size, 0)
    d = ImageDraw.Draw(m)
    for i in range(n):
        a = 2 * math.pi * i / n
        a1, a2 = a - wd, a + wd
        d.polygon([(cx + r0 * math.cos(a), cy + r0 * math.sin(a)),
                   (cx + r1 * math.cos(a1), cy + r1 * math.sin(a1)),
                   (cx + r1 * math.cos(a2), cy + r1 * math.sin(a2))], fill=255)
    fade = Z.radial_mask(img.size, center=(cx / img.width, cy / img.height),
                         radius=(r1 / img.width, r1 / img.height), gamma=1.0)
    m = ImageChops.multiply(m, ImageChops.invert(fade))
    img.alpha_composite(Z.colorize(m.filter(ImageFilter.GaussianBlur(_u(1.5))), color, alpha))


def _ribbon(d, cx, y, half, hgt, fill, edge, tail_drop=0):
    u = _u
    for s in (-1, 1):   # tails
        x0 = cx + s * (half - 10)
        x1 = cx + s * (half + 46)
        tail = [(x0, y + 10 + tail_drop), (x1, y + 10 + tail_drop), (x1 - s * 16, y + 10 + tail_drop + hgt / 2),
                (x1, y + 10 + tail_drop + hgt), (x0, y + 10 + tail_drop + hgt)]
        d.polygon([(u(a), u(b)) for a, b in tail], fill=rgb(Z.mix(fill, "#000000", .25)))
        Z.outline(d, [(u(a), u(b)) for a, b in tail], rgb(edge), u(1.5))
    band = [(cx - half, y), (cx + half, y), (cx + half, y + hgt), (cx - half, y + hgt)]
    d.polygon([(u(a), u(b)) for a, b in band], fill=rgb(fill))
    Z.outline(d, [(u(a), u(b)) for a, b in band], rgb(edge), u(1.5))
    d.line([(u(cx - half + 8), u(y + 6)), (u(cx + half - 8), u(y + 6))], fill=rgb(edge), width=int(u(1)))
    d.line([(u(cx - half + 8), u(y + hgt - 6)), (u(cx + half - 8), u(y + hgt - 6))], fill=rgb(edge), width=int(u(1)))


def _medallion(d, cx, cy, R, metal_hi, metal, metal_dk, inner_fill, accent):
    u = _u
    d.polygon(Z.star8(u(cx), u(cy), u(R), 22.5), fill=rgb(metal_dk))
    d.polygon(Z.star8(u(cx), u(cy), u(R - 5), 22.5), fill=rgb(metal))
    d.polygon(Z.star8(u(cx), u(cy), u(R)), fill=rgb(metal_dk))
    d.polygon(Z.star8(u(cx), u(cy), u(R - 5)), fill=rgb(metal))
    Z.circle(d, u(cx), u(cy), u(R * .62), fill=rgb(metal_dk))
    Z.circle(d, u(cx), u(cy), u(R * .62 - 4), fill=rgb(inner_fill))
    Z.circle(d, u(cx), u(cy), u(R * .62 - 9), outline=rgb(metal_hi), width=int(u(2)))
    d.polygon(Z.star8(u(cx), u(cy), u(R * .40)), fill=rgb(metal_hi))
    d.polygon(Z.star8(u(cx), u(cy), u(R * .30)), fill=rgb(inner_fill))
    d.polygon(Z.star8(u(cx), u(cy), u(R * .18), 22.5), fill=rgb(accent))


def _branch(d, cx, cy, side, color, edge, n=7, R=116, a0=105, a1=215):
    """Geometric laurel spray: lozenges laid tangentially along an arc (side -1 left, +1 right)."""
    u = _u
    for i in range(n):
        t = i / (n - 1)
        phi = math.radians(a0 + (a1 - a0) * t)
        if side > 0:
            phi = math.pi - phi
        x, y = cx + R * math.cos(phi), cy + R * math.sin(phi)
        L, W = 20 - 7 * t, 7.5 - 2.5 * t
        rot = phi + math.radians(28) * (1 if side < 0 else -1)
        pts = [(0, -L), (W, 0), (0, L), (-W, 0)]
        P = [(u(x + px * math.cos(rot) - py * math.sin(rot)), u(y + px * math.sin(rot) + py * math.cos(rot)))
             for px, py in pts]
        d.polygon(P, fill=rgb(color))
        Z.outline(d, P, rgb(edge), u(1.2))


def illus_victory(w=512, h=320):
    img = _canvas(w, h)
    cx, cy = w / 2, 140
    _rays(img, _u(cx), _u(cy), 24, _u(40), _u(138), PAL["gold_hi"], 120, 0.06)
    d = ImageDraw.Draw(img)
    _branch(d, cx, cy, -1, PAL["gold"], GOLD_INK)
    _branch(d, cx, cy, 1, PAL["gold"], GOLD_INK)
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).polygon(Z.star8(_u(cx), _u(cy), _u(96)), fill=255)
    img.alpha_composite(Z.shadow(m, _u(8), (0, _u(6)), "#01040C", 140))
    d = ImageDraw.Draw(img)
    _medallion(d, cx, cy, 96, PAL["gold_hi"], PAL["gold"], GOLD_INK, PAL["navy"], PAL["orange"])
    _ribbon(d, cx, 236, 150, 44, PAL["royal"], PAL["silver"])
    for x in (cx - 60, cx, cx + 60):
        d.polygon(Z.ngon(_u(x), _u(258), _u(7), 4), fill=rgb(PAL["gold"]))
    return Z.down(img, SS)


def illus_defeat(w=512, h=320):
    img = _canvas(w, h)
    cx, cy = w / 2, 140
    d = ImageDraw.Draw(img)
    _branch(d, cx, cy, -1, PAL["silver_mid"], PAL["silver_dark"], n=5, a0=120, a1=200)
    _branch(d, cx, cy, 1, PAL["silver_mid"], PAL["silver_dark"], n=5, a0=120, a1=200)
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).polygon(Z.star8(_u(cx), _u(cy), _u(90)), fill=255)
    img.alpha_composite(Z.shadow(m, _u(8), (0, _u(6)), "#01040C", 140))
    d = ImageDraw.Draw(img)
    _medallion(d, cx, cy, 90, PAL["silver"], PAL["silver_mid"], PAL["silver_dark"], PAL["navy_deep"], PAL["silver_dark"])
    _ribbon(d, cx, 232, 140, 42, "#3A4560", PAL["silver_mid"], tail_drop=8)
    return Z.down(img, SS)


ALL = [("avatar_02", avatar_02), ("ui_card_slot", card_slot), ("ui_dealer_chip", dealer_chip),
       ("icon_sound_on", icon_sound_on), ("icon_sound_off", icon_sound_off), ("icon_restart", icon_restart),
       ("ui_btn_secondary", btn_secondary), ("ui_dialog", dialog), ("ui_callout", callout),
       ("illus_victory", illus_victory), ("illus_defeat", illus_defeat)]

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=Z.EXPORTED)
    a = ap.parse_args()
    for name, fn in ALL:
        Z.save(fn(), name + ".png", a.out)
