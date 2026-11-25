"""Generates the NoteHawk icon: src/NoteHawk.Desktop/Assets/notehawk.ico + .png and assets/notehawk-256.png.
Run: python tools/make_icon.py   (needs Pillow)

A violet notebook with binding rings down the left edge and an amber hawk feather on the cover.
"""
from PIL import Image, ImageDraw, ImageFilter
import math, os

S = 1024
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.dirname(here)


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def vgrad(box, top, bottom):
    x0, y0, x1, y1 = box
    g = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    for y in range(int(y0), int(y1) + 1):
        d.line([(x0, y), (x1, y)], fill=lerp(top, bottom, (y - y0) / (y1 - y0)))
    return g


def rounded_mask(box, r):
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius=r, fill=255)
    return m


img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

# Shadow
shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ImageDraw.Draw(shadow).rounded_rectangle([150, 120, 900, 950], radius=90, fill=(0, 0, 0, 120))
img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(30)))

# Page edges peeking out on the right (the paper inside the notebook)
for i, shade in enumerate([(226, 222, 240, 255), (244, 242, 250, 255)]):
    off = 26 - i * 13
    ImageDraw.Draw(img).rounded_rectangle([170 + off, 100 + off // 2, 880 + off, 920 - off // 2], radius=80, fill=shade)

# Cover
cover_box = [150, 90, 870, 920]
cover = vgrad(cover_box, (132, 98, 255, 255), (72, 44, 190, 255))
img.paste(cover, (0, 0), rounded_mask(cover_box, 86))

# Spine band
spine = vgrad([150, 90, 300, 920], (104, 72, 228, 255), (56, 32, 160, 255))
spine_mask = rounded_mask(cover_box, 86)
ImageDraw.Draw(spine_mask).rectangle([300, 0, S, S], fill=0)
img.paste(spine, (0, 0), spine_mask)

# Soft highlight across the top of the cover
hl = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ImageDraw.Draw(hl).rounded_rectangle([170, 100, 850, 380], radius=80, fill=(255, 255, 255, 34))
img.alpha_composite(hl.filter(ImageFilter.GaussianBlur(24)))

# Binding rings
d = ImageDraw.Draw(img)
for i in range(6):
    y = 190 + i * 128
    d.rounded_rectangle([108, y - 22, 238, y + 22], radius=22, fill=(40, 30, 70, 255))
    d.rounded_rectangle([112, y - 18, 232, y + 14], radius=18, fill=(214, 210, 228, 255))
    d.rounded_rectangle([118, y - 14, 226, y + 2], radius=10, fill=(250, 249, 255, 255))

# Feather: a curved shaft with a leaf-shaped vane, amber shading to gold
feather = Image.new("RGBA", (S, S), (0, 0, 0, 0))
fd = ImageDraw.Draw(feather)


def shaft(t):
    # Gentle S-curve from bottom-left to top-right across the cover
    x = 380 + 370 * t + 40 * math.sin(t * math.pi)
    y = 820 - 640 * t - 30 * math.sin(t * math.pi * 1.2)
    return x, y


def normal(t):
    x1, y1 = shaft(max(0, t - 0.01))
    x2, y2 = shaft(min(1, t + 0.01))
    dx, dy = x2 - x1, y2 - y1
    n = math.hypot(dx, dy)
    return -dy / n, dx / n


left, right = [], []
for i in range(101):
    t = 0.14 + 0.86 * i / 100
    w = 92 * math.sin(math.pi * ((t - 0.14) / 0.86) ** 0.65)
    nx, ny = normal(t)
    x, y = shaft(t)
    left.append((x + nx * w * 0.7, y + ny * w * 0.7))
    right.append((x - nx * w, y - ny * w))
vane = left + right[::-1]
fd.polygon(vane, fill=(255, 255, 255, 255))
# Splits in the vane, the way real feathers separate
for t0, side, depth in ((0.42, -1, 0.95), (0.63, 1, 0.9), (0.30, 1, 0.8)):
    x, y = shaft(t0)
    nx, ny = normal(t0)
    w = 92 * math.sin(math.pi * ((t0 - 0.14) / 0.86) ** 0.65) * (0.7 if side > 0 else 1.0)
    ex, ey = x + side * nx * w * 1.2, y + side * ny * w * 1.2
    ix, iy = x + side * nx * w * (1 - depth), y + side * ny * w * (1 - depth)
    fd.polygon([(ix, iy), (ex + 16, ey + 12), (ex + 28, ey - 2)], fill=(0, 0, 0, 0))
vane_mask = feather.split()[3]
colour = vgrad([0, 180, S, 840], (255, 205, 92, 255), (240, 140, 40, 255))
feather = Image.new("RGBA", (S, S), (0, 0, 0, 0))
feather.paste(colour, (0, 0), vane_mask)
fd = ImageDraw.Draw(feather)
# Barbs (light lines) and a couple of notches
for i in range(4, 96, 7):
    t = 0.14 + 0.86 * i / 100
    x, y = shaft(t)
    nx, ny = normal(t)
    w = 92 * math.sin(math.pi * ((t - 0.14) / 0.86) ** 0.65)
    for side, k in ((1, 0.7), (-1, 1.0)):
        ex, ey = x + side * nx * w * k * 0.92, y + side * ny * w * k * 0.92
        # angle barbs toward the tip
        fd.line([(x, y), (ex + 30, ey - 36)], fill=(255, 236, 180, 150), width=5)
feather.putalpha(Image.composite(feather.split()[3], Image.new("L", (S, S), 0), vane_mask))
# Shadow under feather
fs = Image.new("RGBA", (S, S), (0, 0, 0, 0))
fs.paste((20, 10, 60, 110), (0, 0), vane_mask)
img.alpha_composite(fs.filter(ImageFilter.GaussianBlur(16)), (6, 10))
img.alpha_composite(feather)
# Shaft (quill)
pts = [shaft(i / 100) for i in range(0, 101)]
ImageDraw.Draw(img).line(pts, fill=(255, 250, 235, 255), width=14, joint="curve")
x0, y0 = pts[0]
ImageDraw.Draw(img).ellipse([x0 - 7, y0 - 7, x0 + 7, y0 + 7], fill=(255, 250, 235, 255))

out_dir = os.path.join(root, "src", "NoteHawk.Desktop", "Assets")
os.makedirs(out_dir, exist_ok=True)
os.makedirs(os.path.join(root, "assets"), exist_ok=True)
img.resize((256, 256), Image.LANCZOS).save(os.path.join(out_dir, "notehawk.png"))
img.resize((256, 256), Image.LANCZOS).save(os.path.join(root, "assets", "notehawk-256.png"))
img.save(os.path.join(out_dir, "notehawk.ico"), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("icon written")
