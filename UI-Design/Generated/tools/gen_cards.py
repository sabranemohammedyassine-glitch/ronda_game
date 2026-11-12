"""Zellige card generator: suit symbols, blank face, card back, composed faces.

Usage (run from anywhere):
  python gen_cards.py --sample            # base assets + the 6 review faces
  python gen_cards.py --all               # base assets + all 40 faces (default)
  python gen_cards.py --suit copas --rank 5
Options: --out DIR (suits + blank, default UI-Design/Exported), --cards-out DIR (faces + back,
         default UI-Design/Exported/cards), --width 360 --height 540,
         --ss 4 (supersampling), --font PATH (numeral font, rasterised only),
         --no-base (skip suit symbols / blank face / back)
Faces are composed from data (suit, rank); no per-card manual work.
"""
import argparse
import math
import os
from PIL import Image, ImageDraw
import zellige_common as Z
from zellige_common import PAL, SUITS, rgb

SAMPLE = [("oros", 1), ("copas", 5), ("espadas", 7), ("bastos", 4), ("oros", 10), ("espadas", 12)]


# ------------------------------------------------------------------ suits
def _suit_layer(suit, S):
    """Draw suit symbol (no outline) on an SxS RGBA canvas (hi-res px)."""
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = SUITS[suit]
    main, dark, light = rgb(c["main"]), rgb(c["dark"]), rgb(c["light"])
    gold, gold_hi = rgb(PAL["gold"]), rgb(PAL["gold_hi"])
    u = lambda v: v * S
    P = lambda pts: [(u(x), u(y)) for x, y in pts]

    if suit == "oros":  # coin with zellige star
        Z.circle(d, u(.5), u(.5), u(.43), fill=light)
        Z.circle(d, u(.5), u(.5), u(.355), fill=dark)
        Z.circle(d, u(.5), u(.5), u(.325), fill=main)
        d.polygon(Z.star8(u(.5), u(.5), u(.25), 22.5), fill=dark)
        d.polygon(Z.star8(u(.5), u(.5), u(.20), 22.5), fill=rgb("#FFF4CC"))
        Z.circle(d, u(.5), u(.5), u(.065), fill=dark)
    elif suit == "copas":  # chalice
        bowl = Z.arc_pts(.5, .15, .30, .42, 0, 180)
        d.polygon(P(bowl), fill=main)
        # soft highlight on the bowl
        d.polygon(P(Z.arc_pts(.5, .15, .215, .32, 100, 165) + Z.arc_pts(.5, .15, .16, .26, 165, 100)), fill=light)
        d.rectangle([u(.455), u(.55), u(.545), u(.80)], fill=main)
        base = Z.arc_pts(.5, .91, .26, .15, 180, 360)
        d.polygon(P(base), fill=main)
        d.rounded_rectangle([u(.24), u(.885), u(.76), u(.935)], radius=u(.02), fill=main)
        d.ellipse([u(.395), u(.615), u(.605), u(.715)], fill=gold)          # knot
        d.rounded_rectangle([u(.165), u(.085), u(.835), u(.195)], radius=u(.05), fill=gold)  # rim
        d.rounded_rectangle([u(.21), u(.115), u(.79), u(.145)], radius=u(.015), fill=gold_hi)
    elif suit == "espadas":  # straight sword, point up
        blade = [(.5, .025), (.585, .15), (.575, .665), (.425, .665), (.415, .15)]
        d.polygon(P(blade), fill=main)
        d.polygon(P([(.5, .09), (.522, .16), (.518, .64), (.482, .64), (.478, .16)]), fill=light)
        d.rounded_rectangle([u(.455), u(.70), u(.545), u(.90)], radius=u(.02), fill=dark)   # grip
        d.rounded_rectangle([u(.23), u(.645), u(.77), u(.73)], radius=u(.04), fill=gold)    # guard
        Z.circle(d, u(.225), u(.6875), u(.058), fill=gold)
        Z.circle(d, u(.775), u(.6875), u(.058), fill=gold)
        d.rounded_rectangle([u(.30), u(.672), u(.70), u(.698)], radius=u(.012), fill=gold_hi)
        Z.circle(d, u(.5), u(.915), u(.062), fill=gold)                                    # pommel
    elif suit == "bastos":  # heavy-headed wooden club, set on a diagonal (distinct from the upright sword)
        def sm(t):
            t = max(0.0, min(1.0, t))
            return t * t * (3 - 2 * t)
        n = 60
        y0, y1 = .08, .93
        left, right = [], []
        for i in range(n + 1):
            t = i / n
            hw = .052 + .118 * (1 - sm((t - .10) / .66))
            y = y0 + t * (y1 - y0)
            left.append((.5 - hw, y))
            right.append((.5 + hw, y))
        body = Z.arc_pts(.5, y0, .17, .075, 180, 360) + right + Z.arc_pts(.5, y1, .052, .04, 0, 180) + left[::-1]
        d.polygon(P(body), fill=main)
        # wood-grain highlights on the head
        d.polygon(P(Z.arc_pts(.5, .22, .12, .19, 150, 215) + Z.arc_pts(.5, .22, .085, .16, 215, 150)), fill=light)
        for (cx, cy, r) in [(.56, .15, .036), (.535, .32, .028), (.515, .50, .02)]:
            d.ellipse([u(cx - r), u(cy - r * .62), u(cx + r), u(cy + r * .62)], fill=dark)
        # gold grip band
        d.rounded_rectangle([u(.435), u(.86), u(.565), u(.915)], radius=u(.018), fill=gold)
        d.rounded_rectangle([u(.45), u(.875), u(.55), u(.89)], radius=u(.007), fill=gold_hi)
        img = img.rotate(-32, Image.BICUBIC)
    return img


_cache = {}
PIP_SCALE = {"oros": 0.88}   # the coin fills its box, so shrink it a little in pip grids


def suit_hi(suit, S):
    """Outlined suit symbol at hi-res size S (px). Cached."""
    key = (suit, S)
    if key not in _cache:
        _cache[key] = Z.with_outline(_suit_layer(suit, S), SUITS[suit]["dark"], max(2.0, S * 0.022))
    return _cache[key]


def export_suits(out, size=256, ss=4):
    for s in Z.SUIT_ORDER:
        Z.save(Z.down(suit_hi(s, size * ss), ss), f"suit_{s}.png", out)


# ------------------------------------------------------------------ card base
class Card:
    def __init__(self, W=360, H=540, ss=4, font_path=Z.FONT_NUMERAL):
        self.W, self.H, self.ss, self.font_path = W, H, ss, font_path
        self.k = W / 360.0 * ss            # design units (360x540) -> hi-res px
        self.size = (W * ss, H * ss)
        self.radius = 26 * self.k
        self.mask = Z.rr_mask(self.size, [0, 0, self.size[0] - 1, self.size[1] - 1], self.radius)

    def u(self, v):
        return v * self.k

    def finish(self, img):
        img.putalpha(Image.composite(img.getchannel("A"), Image.new("L", self.size, 0), self.mask))
        return Z.down(img, self.ss)

    def _stock(self):
        u = self.u
        w, h = self.size
        img = Z.vgrad(self.size, PAL["ivory_hi"], PAL["ivory"]).convert("RGBA")
        d = ImageDraw.Draw(img)
        e = u(3)
        d.rounded_rectangle([e / 2, e / 2, w - 1 - e / 2, h - 1 - e / 2], radius=self.radius - e / 2,
                            outline=rgb(PAL["ivory_edge"]), width=int(e))
        return img, d

    # blank ivory face (hi-res, before downsample)
    def blank_hi(self, court=False):
        u = self.u
        w, h = self.size
        img, d = self._stock()
        i = u(13)
        d.rounded_rectangle([i, i, w - 1 - i, h - 1 - i], radius=u(15), outline=rgb(PAL["ivory_line"]),
                            width=int(u(2)))
        if court:
            i = u(19)
            d.rounded_rectangle([i, i, w - 1 - i, h - 1 - i], radius=u(10), outline=rgb(PAL["gold"]),
                                width=int(u(1.5)))
        return img

    def back(self):
        u = self.u
        w, h = self.size
        img, d = self._stock()
        # navy field with star-and-cross zellige
        inset = u(13)
        fmask = Z.rr_mask(self.size, [inset, inset, w - 1 - inset, h - 1 - inset], u(15))
        field = Z.vgrad(self.size, PAL["navy_mid"], PAL["navy"]).convert("RGBA")
        fd = ImageDraw.Draw(field)
        P = u(48)
        cx0, cy0 = w / 2, h / 2
        for j in range(-7, 8):
            for i in range(-5, 6):
                x, y = cx0 + i * P, cy0 + j * P
                pts = Z.star8(x, y, P / 2 - u(1.2))
                fd.polygon(pts, fill=rgb(PAL["royal"]))
                Z.outline(fd, pts, Z.mix(PAL["silver"], PAL["royal"], .25), u(1.6))
                fd.polygon(Z.star8(x, y, u(6.5), 22.5), fill=rgb(PAL["navy"]))
        for j in range(-7, 8):          # small gold studs in the cross centres
            for i in range(-5, 6):
                x, y = cx0 + (i + .5) * P, cy0 + (j + .5) * P
                fd.polygon(Z.ngon(x, y, u(3.6), 4), fill=rgb(PAL["gold"]))
        # central medallion
        Z.circle(fd, cx0, cy0, u(86), fill=rgb(PAL["navy"]))
        Z.circle(fd, cx0, cy0, u(86), outline=rgb(PAL["silver"]), width=int(u(3)))
        Z.circle(fd, cx0, cy0, u(78), outline=rgb(PAL["gold"]), width=int(u(1.5)))
        Z.outline(fd, Z.star8(cx0, cy0, u(66)), rgb(PAL["silver"]), u(3))
        Z.outline(fd, Z.star8(cx0, cy0, u(66), 22.5), rgb(PAL["silver"]), u(3))
        fd.polygon(Z.star8(cx0, cy0, u(40)), fill=rgb(PAL["gold"]))
        fd.polygon(Z.star8(cx0, cy0, u(31)), fill=rgb(PAL["navy"]))
        fd.polygon(Z.star8(cx0, cy0, u(19), 22.5), fill=rgb(PAL["gold_hi"]))
        # silver frame lines
        for ins, wd, col in [(13, 3, PAL["silver_mid"]), (21, 2, PAL["silver"])]:
            fd.rounded_rectangle([u(ins), u(ins), w - 1 - u(ins), h - 1 - u(ins)], radius=u(15 - (ins - 13) * .6),
                                 outline=rgb(col), width=int(u(wd)))
        img.paste(field, (0, 0), fmask)
        return self.finish(img)

    # ---------------------------------------------------------- face parts
    def _corner(self, suit, rank):
        u = self.u
        layer = Image.new("RGBA", self.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        txt = str(rank)
        size = u(64)
        f = Z.font(self.font_path, size)
        two = len(txt) > 1
        maxw = u(64) if two else u(56)
        bb = d.textbbox((0, 0), txt, font=f)
        if bb[2] - bb[0] > maxw:
            size *= maxw / (bb[2] - bb[0])
            f = Z.font(self.font_path, size)
            bb = d.textbbox((0, 0), txt, font=f)
        cx = u(55) if two else u(48)
        top = u(27)
        d.text((cx - (bb[0] + bb[2]) / 2, top - bb[1]), txt, font=f, fill=rgb(SUITS[suit]["dark"]))
        cap_h = bb[3] - bb[1]
        ps = int(u(44))
        Z.paste_center(layer, suit_hi(suit, ps), cx, top + max(cap_h, u(38)) + u(9) + ps / 2)
        return layer

    def _ornament(self, kind, S):
        """Court top ornament on SxS canvas: star (10), horseshoe (11), crown (12)."""
        img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        gold, gold_hi = rgb(PAL["gold"]), rgb(PAL["gold_hi"])
        v = lambda x: x * S
        if kind == 10:
            d.polygon(Z.star8(v(.5), v(.5), v(.36), 22.5), fill=gold)
            d.polygon(Z.star8(v(.5), v(.5), v(.17)), fill=gold_hi)
            oc = "#5E4510"
        elif kind == 11:
            outer = Z.arc_pts(.5, .47, .36, .36, 125, 415)
            inner = Z.arc_pts(.5, .47, .20, .20, 415, 125)
            d.polygon([(v(x), v(y)) for x, y in outer + inner], fill=rgb(PAL["silver"]))
            for a in (165, 215, 270, 325, 375):
                x, y = .5 + .28 * math.cos(math.radians(a)), .47 + .28 * math.sin(math.radians(a))
                Z.circle(d, v(x), v(y), v(.032), fill=rgb(PAL["silver_dark"]))
            oc = PAL["silver_dark"]
        else:
            pts = [(.12, .78), (.08, .30), (.29, .52), (.5, .16), (.71, .52), (.92, .30), (.88, .78)]
            d.polygon([(v(x), v(y)) for x, y in pts], fill=gold)
            d.rounded_rectangle([v(.12), v(.68), v(.88), v(.84)], radius=v(.03), fill=gold_hi)
            for x, y in [(.08, .28), (.5, .14), (.92, .28)]:
                Z.circle(d, v(x), v(y), v(.065), fill=gold_hi)
            for x in (.3, .5, .7):
                d.polygon(Z.ngon(v(x), v(.76), v(.045), 4), fill=rgb(PAL["orange"]))
            oc = "#5E4510"
        return Z.with_outline(img, oc, max(2.0, S * 0.03))

    def face(self, suit, rank):
        if suit not in SUITS or rank not in Z.RANKS:
            raise ValueError(f"invalid card {suit} {rank}")
        u = self.u
        w, h = self.size
        court = rank >= 10
        img = self.blank_hi(court)
        d = ImageDraw.Draw(img)
        c = SUITS[suit]
        cx, cy = w / 2, h / 2

        if not court:
            lx, rx = 127, 233
            layouts = {
                1: (228, [(180, 270)]),
                2: (132, [(180, 165), (180, 375)]),
                3: (118, [(180, 132), (180, 270), (180, 408)]),
                4: (122, [(lx, 172), (rx, 172), (lx, 368), (rx, 368)]),
                5: (110, [(lx, 160), (rx, 160), (180, 270), (lx, 380), (rx, 380)]),
                6: (104, [(lx, 136), (rx, 136), (lx, 270), (rx, 270), (lx, 404), (rx, 404)]),
                7: (96, [(lx, 134), (rx, 134), (96, 270), (180, 270), (264, 270), (lx, 406), (rx, 406)]),
            }
            size, pos = layouts[rank]
            if rank == 1:   # ace: quiet zellige halo behind the large symbol
                tint = Z.mix(PAL["ivory"], c["main"], .16)
                d.polygon(Z.star8(cx, cy, u(150)), fill=tint)
                Z.outline(d, Z.star8(cx, cy, u(150)), rgb(PAL["ivory_line"]), u(2))
                for dy in (-178, 178):
                    d.polygon(Z.ngon(cx, cy + u(dy), u(9), 4), fill=rgb(PAL["gold"]))
            sym = suit_hi(suit, int(u(size * PIP_SCALE.get(suit, 1.0))))
            for (x, y) in pos:
                Z.paste_center(img, sym, u(x), u(y))
        else:
            mcy = u(282)
            tint = Z.mix(PAL["ivory"], c["main"], .20)
            R = u(124)
            d.polygon(Z.star8(cx, mcy, R), fill=tint)
            Z.outline(d, Z.star8(cx, mcy, R), rgb(c["dark"]), u(4))
            Z.outline(d, Z.star8(cx, mcy, R - u(13)), rgb(PAL["gold"]), u(2))
            Z.paste_center(img, suit_hi(suit, int(u(168))), cx, mcy)
            Z.paste_center(img, self._ornament(rank, int(u(78))), cx, u(112))
            n = rank - 9    # 1, 2 or 3 gold diamonds = quick rank read
            dia = Image.new("RGBA", (int(u(30)), int(u(30))), (0, 0, 0, 0))
            ImageDraw.Draw(dia).polygon(Z.ngon(dia.width / 2, dia.height / 2, u(10), 4), fill=rgb(PAL["gold"]))
            dia = Z.with_outline(dia, "#5E4510", u(1.6))
            for i in range(n):
                Z.paste_center(img, dia, cx + (i - (n - 1) / 2) * u(30), u(446))

        corner = self._corner(suit, rank)
        img.alpha_composite(corner)
        img.alpha_composite(corner.rotate(180))
        return self.finish(img)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--all", action="store_true", help="generate all 40 faces")
    ap.add_argument("--sample", action="store_true", help="generate the 6 review faces")
    ap.add_argument("--suit", choices=Z.SUIT_ORDER)
    ap.add_argument("--rank", type=int, choices=Z.RANKS)
    ap.add_argument("--out", default=Z.EXPORTED, help="root for suit symbols + blank face")
    ap.add_argument("--cards-out", default=os.path.join(Z.EXPORTED, "cards"), help="folder for faces + back")
    ap.add_argument("--width", type=int, default=360)
    ap.add_argument("--height", type=int, default=540)
    ap.add_argument("--ss", type=int, default=4)
    ap.add_argument("--font", default=Z.FONT_NUMERAL)
    ap.add_argument("--no-base", action="store_true", help="skip suits / blank / back")
    a = ap.parse_args()
    card = Card(a.width, a.height, a.ss, a.font)
    if a.all:
        todo = [(s, r) for s in Z.SUIT_ORDER for r in Z.RANKS]
    elif a.sample:
        todo = SAMPLE
    elif a.suit and a.rank:
        todo = [(a.suit, a.rank)]
    else:
        todo = [(s, r) for s in Z.SUIT_ORDER for r in Z.RANKS]
    if not a.no_base:
        export_suits(a.out)
        Z.save(card.finish(card.blank_hi()), "card_face_blank.png", a.out)
        Z.save(card.back(), "card_back.png", a.cards_out)
    for s, r in todo:
        Z.save(card.face(s, r), f"card_{s}_{r:02d}.png", a.cards_out)


if __name__ == "__main__":
    main()
