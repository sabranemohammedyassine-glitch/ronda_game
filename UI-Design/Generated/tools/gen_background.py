"""Zellige table background: bg_table_main.png (1920x1080, opaque, no text).

Usage: python gen_background.py [--out DIR]
Layers: radial-lit navy felt -> faint star-and-cross field (fades to nothing in the
centre) -> silver zellige frame band with gold corner medallions -> vignette.
"""
import argparse
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import zellige_common as Z
from zellige_common import PAL, rgb

W, H = 1920, 1080
P = 40          # star pitch of the frame band (1840/40 and 1000/40 are integers)
M = 40          # band centre-line inset from the screen edge


def felt():
    edge = Image.new("RGB", (W, H), rgb(PAL["navy_deep"]))
    mid = Image.new("RGB", (W, H), rgb(PAL["navy_mid"]))
    lit = Image.new("RGB", (W, H), rgb("#1B438A"))
    img = Image.composite(edge, mid, Z.radial_mask((W, H), radius=(0.62, 0.86), gamma=1.5))
    glow = Z.radial_mask((W, H), radius=(0.42, 0.56), gamma=0.9)
    img = Image.composite(img, lit, glow.point(lambda v: 70 + v * 185 // 255))
    # fine felt grain (also dithers the gradient against banding)
    noise = Image.effect_noise((W, H), 14).convert("RGB")
    img = Image.blend(img, ImageChops.overlay(img, noise), 0.35)
    return img.convert("RGBA")


def field_pattern(ss=2):
    """Very faint star-and-cross lattice, strongest near the frame, absent in the centre."""
    m = Image.new("L", (W * ss, H * ss), 0)
    d = ImageDraw.Draw(m)
    pitch = 120 * ss
    for j in range(-1, H * ss // pitch + 2):
        for i in range(-1, W * ss // pitch + 2):
            x, y = W * ss / 2 + (i - W * ss // pitch // 2) * pitch, H * ss / 2 + (j - H * ss // pitch // 2) * pitch
            Z.outline(d, Z.star8(x, y, pitch / 2 - 2 * ss), 255, 1.5 * ss)
            Z.outline(d, Z.star8(x, y, pitch * 0.2, 22.5), 255, 1.2 * ss)
    m = Z.down(m, ss)
    fade = Z.radial_mask((W, H), radius=(0.60, 0.80), gamma=2.2)   # 0 in centre
    m = ImageChops.multiply(m, fade)
    return Z.colorize(m, PAL["silver"], 34)


def frame(ss=2):
    w, h = W * ss, H * ss
    u = lambda v: v * ss
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    # outer rail
    rail = Image.new("L", (w, h), 255)
    ImageDraw.Draw(rail).rounded_rectangle([u(M - 26), u(M - 26), w - u(M - 26), h - u(M - 26)], radius=u(10), fill=0)
    layer.alpha_composite(Z.colorize(rail, "#050C1B", 235))
    # band bed
    d.rectangle([u(M - 22), u(M - 22), w - u(M - 22), h - u(M - 22)], fill=rgb("#0A1731") + (255,))
    hole = [u(M + 22), u(M + 22), w - u(M + 22), h - u(M + 22)]
    # stars chain along the four sides
    pts = []
    nx, ny = (W - 2 * M) // P, (H - 2 * M) // P
    for i in range(nx + 1):
        pts += [(M + i * P, M), (M + i * P, H - M)]
    for j in range(1, ny):
        pts += [(M, M + j * P), (W - M, M + j * P)]
    line = Z.mix(PAL["silver"], PAL["navy"], .12)
    for (x, y) in pts:
        s = Z.star8(u(x), u(y), u(P / 2 - 1))
        d.polygon(s, fill=rgb("#17376F"))
        Z.outline(d, s, line, u(1.5))
        d.polygon(Z.star8(u(x), u(y), u(5.2), 22.5), fill=rgb("#0A1731"))
        Z.outline(d, Z.star8(u(x), u(y), u(5.2), 22.5), rgb(PAL["silver_mid"]), u(1))
    # punch the playfield back out, then rule lines
    cut = Image.new("L", (w, h), 255)
    ImageDraw.Draw(cut).rectangle(hole, fill=0)
    layer.putalpha(ImageChops.multiply(layer.getchannel("A"), cut))
    d = ImageDraw.Draw(layer)
    for ins, wd, col in [(M - 24, 2, PAL["silver"]), (M + 22, 2, PAL["silver"]), (M + 28, 1, PAL["gold"])]:
        d.rectangle([u(ins), u(ins), w - u(ins) - 1, h - u(ins) - 1], outline=rgb(col), width=int(u(wd)))
    # corner medallions (gold accent)
    for (x, y) in [(M, M), (W - M, M), (M, H - M), (W - M, H - M)]:
        cx, cy = u(x), u(y)
        Z.circle(d, cx, cy, u(31), fill=rgb(PAL["navy_deep"]), outline=rgb(PAL["silver"]), width=int(u(2)))
        d.polygon(Z.star8(cx, cy, u(23)), fill=rgb(PAL["gold"]))
        d.polygon(Z.star8(cx, cy, u(17.5)), fill=rgb(PAL["navy_deep"]))
        d.polygon(Z.star8(cx, cy, u(10.5), 22.5), fill=rgb(PAL["gold_hi"]))
    # mid-edge accents: small gold diamonds at the centre of each side
    for (x, y) in [(W / 2, M + 28), (W / 2, H - M - 28), (M + 28, H / 2), (W - M - 28, H / 2)]:
        d.polygon(Z.ngon(u(x), u(y), u(7), 4), fill=rgb(PAL["gold"]))
        d.polygon(Z.ngon(u(x), u(y), u(3), 4), fill=rgb(PAL["navy_deep"]))
    return Z.down(layer, ss)


def build():
    img = felt()
    img.alpha_composite(field_pattern())
    fr = frame()
    # soft inner shadow cast by the frame onto the felt
    sh = fr.getchannel("A").filter(ImageFilter.GaussianBlur(16))
    img.alpha_composite(Z.colorize(sh, "#020610", 150))
    img.alpha_composite(fr)
    return img.convert("RGB")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=Z.EXPORTED)
    a = ap.parse_args()
    Z.save(build(), "bg_table_main.png", a.out)
