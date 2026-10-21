# 컷씬 그림을 배경과 캐릭터로 나눠 픽셀 격자에 맞추고 합친다.
#   배경: 원래 격자로 복원 → 색 줄이기 → 대비·채도 올리기 (테두리 없음, 부드러운 거리감)
#   캐릭터: 마젠타 배경 지우기 → 원래 격자로 복원 → 바깥 1픽셀 검은 테두리
#   합치기: 캐릭터를 배경 격자 1:1로 얹는다(크기를 억지로 바꾸면 픽셀이 깨지므로 바꾸지 않는다)
import os

import numpy as np
from PIL import Image, ImageEnhance
from scipy import ndimage
from scipy.cluster.vq import kmeans2

EXPORT = "ArtSource/Export"
OUTLINE = (28, 18, 14)
SCALE = 8


def _find_offset(a, w, h):
    H0, W0 = a.shape[:2]
    sx, sy = W0 / w, H0 / h
    best = None
    for ox in np.arange(0, sx, 0.5):
        for oy in np.arange(0, sy, 0.5):
            cx = (ox + (np.arange(w) + 0.5) * sx).astype(int).clip(0, W0 - 1)
            cy = (oy + (np.arange(h) + 0.5) * sy).astype(int).clip(0, H0 - 1)
            ix = ((np.arange(W0) - ox) // sx).astype(int).clip(0, w - 1)
            iy = ((np.arange(H0) - oy) // sy).astype(int).clip(0, h - 1)
            err = np.abs(a[np.ix_(cy, cx)][np.ix_(iy, ix)] - a).mean()
            if best is None or err < best[0]:
                best = (err, ox, oy)
    return best[1], best[2]


def _blocks(a, w, h):
    # 블록 안쪽(가장자리 번짐 제외)의 중앙값. 검은 선이 블록에 충분히 걸치면 선 색으로.
    H0, W0 = a.shape[:2]
    sx, sy = W0 / w, H0 / h
    ox, oy = _find_offset(a, w, h)
    lum = a[..., :3] @ [0.299, 0.587, 0.114]
    out = np.zeros((h, w, a.shape[2]))
    for j in range(h):
        y0 = int(np.clip(oy + j * sy, 0, H0 - 1)); y1 = int(np.clip(oy + (j + 1) * sy, y0 + 1, H0))
        for i in range(w):
            x0 = int(np.clip(ox + i * sx, 0, W0 - 1)); x1 = int(np.clip(ox + (i + 1) * sx, x0 + 1, W0))
            dark = lum[y0:y1, x0:x1].ravel() < 40
            block = a[y0:y1, x0:x1].reshape(-1, a.shape[2])
            if dark.mean() >= 0.35:
                out[j, i] = np.median(block[dark], axis=0)
                continue
            m = 0.2
            yy0 = int(y0 + (y1 - y0) * m); yy1 = max(yy0 + 1, int(y1 - (y1 - y0) * m))
            xx0 = int(x0 + (x1 - x0) * m); xx1 = max(xx0 + 1, int(x1 - (x1 - x0) * m))
            out[j, i] = np.median(a[yy0:yy1, xx0:xx1].reshape(-1, a.shape[2]), axis=0)
    return out


def _quantize(rgb, k):
    flat = rgb.reshape(-1, 3)
    np.random.seed(0)
    centers, labels = kmeans2(flat, k, minit="++", iter=40, seed=0)
    return centers.clip(0, 255)[labels].reshape(rgb.shape)


def background(src, w=240, h=135, colors=20, contrast=1.15, saturation=1.1):
    a = np.asarray(Image.open(src).convert("RGB")).astype(float)
    rgb = _blocks(a, w, h)
    img = Image.fromarray(rgb.clip(0, 255).astype(np.uint8))
    img = ImageEnhance.Color(ImageEnhance.Contrast(img).enhance(contrast)).enhance(saturation)
    q = _quantize(np.asarray(img).astype(float), colors)
    return Image.fromarray(q.astype(np.uint8))


def character(src, w, h):
    a = np.asarray(Image.open(src).convert("RGB")).astype(float)
    rgb = _blocks(a, w, h)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    opaque = (np.minimum(r, b) - g) < 90
    lab, n = ndimage.label(opaque)
    sizes = ndimage.sum(opaque, lab, range(1, n + 1))
    opaque = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= 3])
    # 마젠타가 섞인 가장자리 칸은 지운다
    pink = opaque & ((np.minimum(r, b) - g) > 15) & (b > g)
    opaque &= ~(pink & ndimage.binary_dilation(~opaque))
    rgba = np.zeros((h, w, 4), np.uint8)
    rgba[..., :3] = rgb.clip(0, 255).astype(np.uint8)
    rgba[..., 3] = np.where(opaque, 255, 0)
    # 테두리 1픽셀: 실루엣 가장자리 칸 자체를 검은 선으로 바꾼다. 원본에 이미 외곽선이 있어
    # 바깥에 한 줄을 덧대면 두 줄이 되므로 안쪽 가장자리만 칠해 늘 한 줄로 맞춘다.
    # 가장자리 바로 안쪽에 이미 검은 선이 있으면, 바깥 가장자리 칸은 번진 찌꺼기라 지운다(원래 선이 테두리가 된다).
    lum = rgb @ [0.299, 0.587, 0.114]
    dark = opaque & (lum < 60)
    edge = opaque & ~ndimage.binary_erosion(opaque)
    inner_dark = ndimage.binary_dilation(dark & ~edge, structure=[[0, 1, 0], [1, 1, 1], [0, 1, 0]])
    shave = edge & ~dark & inner_dark
    opaque &= ~shave
    rgba[..., 3] = np.where(opaque, 255, 0)
    edge = opaque & ~ndimage.binary_erosion(opaque)
    rgba[edge, :3] = OUTLINE
    ys, xs = np.where(rgba[..., 3] > 0)
    return Image.fromarray(rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1], "RGBA")


def compose(bg, char, x, y, name, flip=False):
    if flip:
        char = char.transpose(Image.FLIP_LEFT_RIGHT)
    out = bg.convert("RGBA")
    out.alpha_composite(char, (x, y))
    out = out.convert("RGB")
    os.makedirs(EXPORT, exist_ok=True)
    out.save(f"{EXPORT}/{name}_1x.png")
    out.resize((out.width * SCALE, out.height * SCALE), Image.NEAREST).save(f"{EXPORT}/{name}_8x.png")
    return out
