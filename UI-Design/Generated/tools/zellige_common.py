"""Shared drawing helpers and palette for the Zellige procedural asset generators.

All art is drawn procedurally (no external images). Shapes are rendered on a
supersampled canvas (SS x) and downsampled with LANCZOS for clean edges.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

TOOLS = os.path.dirname(os.path.abspath(__file__))
GENERATED = os.path.dirname(TOOLS)
UI_ROOT = os.path.dirname(GENERATED)
EXPORTED = os.path.join(UI_ROOT, "Exported")

SS = 4  # default supersampling factor


def rgb(h):
    if isinstance(h, tuple):
        return tuple(h[:3])
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def rgba(h, a=255):
    return rgb(h) + (a,)


def mix(c1, c2, t):
    a, b = rgb(c1) if isinstance(c1, str) else c1, rgb(c2) if isinstance(c2, str) else c2
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


PAL = {
    # table
    "navy_deep": "#081226", "navy": "#0E1F42", "navy_mid": "#143060",
    "royal": "#1E4A94", "royal_light": "#2F66C0",
    # metals
    "silver_hi": "#EEF2F7", "silver": "#C3CCD9", "silver_mid": "#8E9AAD", "silver_dark": "#566175",
    "gold_hi": "#F3D584", "gold": "#D6A93F", "gold_dark": "#9A7422",
    "orange": "#E8792B", "orange_hi": "#F5A25A",
    # card stock
    "ivory_hi": "#FCF8EC", "ivory": "#F6EFDC", "ivory_edge": "#B8AC8E", "ivory_line": "#C9B37A",
    "ink": "#1B2238",
    # text
    "text": "#F4F6FA", "text_2": "#AEB9CC", "text_dim": "#6F7C94",
    # states
    "ok": "#3FB67B", "warn": "#F0B429", "danger": "#E0524A", "disabled": "#5A6478",
}

# suit -> (main fill, dark ink/outline, light tint accent)
SUITS = {
    "oros":    {"main": "#E2A626", "dark": "#7C5206", "light": "#F7D676"},
    "copas":   {"main": "#BF3440", "dark": "#7E1822", "light": "#E8808A"},
    "espadas": {"main": "#2B62A8", "dark": "#15345F", "light": "#8FB4E3"},
    "bastos":  {"main": "#2F8552", "dark": "#174A2C", "light": "#7CC492"},
}
SUIT_ORDER = ["oros", "copas", "espadas", "bastos"]
RANKS = [1, 2, 3, 4, 5, 6, 7, 10, 11, 12]

FONT_DIR = r"C:\Windows\Fonts"
FONT_NUMERAL = os.path.join(FONT_DIR, "cambriab.ttf")   # baked into card art only
FONT_MOCK_UI = os.path.join(FONT_DIR, "segoeuib.ttf")   # mock-up only
FONT_MOCK_UI_REG = os.path.join(FONT_DIR, "segoeui.ttf")


def font(path, size):
    return ImageFont.truetype(path, int(round(size)))


# ---------------------------------------------------------------- geometry
INNER8 = math.cos(math.pi / 4) / math.cos(math.pi / 8)  # 0.7654: octagram of 2 squares


def star8(cx, cy, R, rot=0.0, inner=INNER8):
    pts = []
    for i in range(16):
        r = R if i % 2 == 0 else R * inner
        a = math.radians(rot) + i * math.pi / 8
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def ngon(cx, cy, R, n, rot=0.0):
    return [(cx + R * math.cos(math.radians(rot) + 2 * math.pi * i / n),
             cy + R * math.sin(math.radians(rot) + 2 * math.pi * i / n)) for i in range(n)]


def arc_pts(cx, cy, rx, ry, a0, a1, n=48):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + ry * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def outline(d, pts, color, w):
    """Closed stroked polygon with rounded joints."""
    pts = list(pts)
    d.line(pts + [pts[0], pts[1]], fill=color, width=max(1, int(round(w))), joint="curve")


def circle(d, cx, cy, r, **kw):
    d.ellipse([cx - r, cy - r, cx + r, cy + r], **kw)


# ---------------------------------------------------------------- raster helpers
def down(img, ss=SS):
    return img.resize((img.width // ss, img.height // ss), Image.LANCZOS)


def vgrad(size, top, bottom):
    """Vertical gradient RGB image."""
    m = Image.linear_gradient("L").resize(size, Image.BICUBIC)
    return Image.composite(Image.new("RGB", size, rgb(bottom)), Image.new("RGB", size, rgb(top)), m)


def radial_mask(size, center=(0.5, 0.5), radius=(0.75, 0.75), gamma=1.0):
    """L mask: 0 at center -> 255 at/after radius (fractions of width/height)."""
    w, h = size
    g = Image.new("L", (256, 256))  # 0 centre .. 255 exactly at the edge mid-points (clamped beyond)
    g.putdata([min(255, int(math.hypot(x - 127.5, y - 127.5) * 2.0)) for y in range(256) for x in range(256)])
    rw, rh = int(2 * radius[0] * w), int(2 * radius[1] * h)
    g = g.resize((rw, rh), Image.BICUBIC)
    out = Image.new("L", size, 255)
    out.paste(g, (int(center[0] * w - rw / 2), int(center[1] * h - rh / 2)))
    if gamma != 1.0:
        out = out.point(lambda v: int(255 * (v / 255) ** gamma))
    return out


def rr_mask(size, box, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=radius, fill=255)
    return m


def dilate(mask, r):
    """Approximate round dilation of an L mask by r pixels."""
    if r <= 0:
        return mask
    b = mask.filter(ImageFilter.GaussianBlur(r * 0.55))
    return b.point(lambda v: 255 if v > 18 else 0)


def colorize(mask, color, alpha=255):
    """RGBA image of flat colour using mask as alpha."""
    img = Image.new("RGBA", mask.size, rgb(color) + (0,))
    a = mask if alpha == 255 else mask.point(lambda v: v * alpha // 255)
    img.putalpha(a)
    return img


def with_outline(img, color, r):
    """Put a dark outline (dilated alpha) under an RGBA image."""
    a = img.getchannel("A")
    out = colorize(dilate(a, r), color)
    out.alpha_composite(img)
    return out


def shadow(mask, blur, offset=(0, 0), color="#000000", alpha=120, expand=0):
    """Soft shadow RGBA same size as mask."""
    m = mask
    if expand:
        m = dilate(m, expand)
    if offset != (0, 0):
        m2 = Image.new("L", m.size, 0)
        m2.paste(m, offset)
        m = m2
    m = m.filter(ImageFilter.GaussianBlur(blur))
    return colorize(m, color, alpha)


def paste_center(dst, src, cx, cy):
    dst.alpha_composite(src, (int(round(cx - src.width / 2)), int(round(cy - src.height / 2))))


def save(img, name, folder=EXPORTED):
    os.makedirs(folder, exist_ok=True)
    p = os.path.join(folder, name)
    img.save(p, optimize=True)
    print("wrote", os.path.relpath(p, UI_ROOT), img.size, img.mode)
    return p
