# 이미 픽셀로 그려진 생성 그림을 원래 격자로 되살린다.
# 사용: python pixel_restore.py <원본> <출력이름> <가로칸> <세로칸> [색수]
# 출력: ArtSource/Export/<출력이름>_1x.png, _8x.png
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage
from scipy.cluster.vq import kmeans2

DARK_LUM = 40        # 이보다 어두우면 외곽선 후보 (진한 갈색 그림자는 빼고 검은 선만)
DARK_SHARE = 0.35    # 블록 안 외곽선 후보 비율이 이 이상이면 외곽선 블록
SCALE = 8


def restore(src, name, w, h, colors=48):
    a = np.asarray(Image.open(src).convert("RGB")).astype(float)
    H0, W0 = a.shape[:2]
    sx, sy = W0 / w, H0 / h
    lum = a @ [0.299, 0.587, 0.114]

    # 격자 시작점: 블록 가운데 색으로 다시 칠했을 때 원본과 가장 비슷한 곳
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
    _, ox, oy = best

    out = np.zeros((h, w, 3))
    for j in range(h):
        y0 = int(np.clip(oy + j * sy, 0, H0 - 1)); y1 = int(np.clip(oy + (j + 1) * sy, y0 + 1, H0))
        for i in range(w):
            x0 = int(np.clip(ox + i * sx, 0, W0 - 1)); x1 = int(np.clip(ox + (i + 1) * sx, x0 + 1, W0))
            block = a[y0:y1, x0:x1].reshape(-1, 3)
            dark = lum[y0:y1, x0:x1].ravel() < DARK_LUM
            # 얇은 외곽선이 밝은 색에 묻히지 않게, 외곽선이 충분히 걸친 블록은 외곽선 색으로
            if dark.mean() >= DARK_SHARE:
                out[j, i] = np.median(block[dark], axis=0)
            else:
                # 가장자리 번짐을 빼려고 블록 안쪽만 본다
                m = 0.2
                yy0 = int(y0 + (y1 - y0) * m); yy1 = max(yy0 + 1, int(y1 - (y1 - y0) * m))
                xx0 = int(x0 + (x1 - x0) * m); xx1 = max(xx0 + 1, int(x1 - (x1 - x0) * m))
                out[j, i] = np.median(a[yy0:yy1, xx0:xx1].reshape(-1, 3), axis=0)

    flat = out.reshape(-1, 3)
    np.random.seed(0)
    centers, labels = kmeans2(flat, colors, minit="++", iter=40, seed=0)
    centers = centers.clip(0, 255)
    img = centers[labels].reshape(h, w, 3)

    # 외곽선 정리: 끊긴 한 칸 메우기, 외톨이 점 지우기
    clum = centers @ [0.299, 0.587, 0.114]
    outline_color = centers[clum.argmin()]
    olum = img @ [0.299, 0.587, 0.114]
    line = olum < DARK_LUM
    pad = np.pad(line, 1)
    # 대각선까지 메우면 바둑판이 생겨 상하좌우만 본다
    pairs = [((0, 1), (2, 1)), ((1, 0), (1, 2))]
    fill = np.zeros_like(line)
    for (ay, ax), (by, bx) in pairs:
        fill |= pad[ay:ay + h, ax:ax + w] & pad[by:by + h, bx:bx + w]
    fill &= ~line
    img[fill] = outline_color
    line |= fill
    neighbors = ndimage.convolve(line.astype(int), np.ones((3, 3), int), mode="constant") - line
    lone = line & (neighbors == 0)
    if lone.any():
        # 외톨이 점은 주변에서 가장 흔한 색으로
        for y, x in zip(*np.where(lone)):
            win = img[max(0, y - 1):y + 2, max(0, x - 1):x + 2].reshape(-1, 3)
            win = win[(win @ [0.299, 0.587, 0.114]) >= DARK_LUM]
            if len(win):
                img[y, x] = np.median(win, axis=0)

    result = Image.fromarray(img.astype(np.uint8))
    os.makedirs("ArtSource/Export", exist_ok=True)
    result.save(f"ArtSource/Export/{name}_1x.png")
    result.resize((w * SCALE, h * SCALE), Image.NEAREST).save(f"ArtSource/Export/{name}_8x.png")
    print(name, "offset", best[1], best[2], "gap fill", int(fill.sum()), "lone", int(lone.sum()))


if __name__ == "__main__":
    restore(sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]),
            int(sys.argv[5]) if len(sys.argv) > 5 else 48)
