# 새로 뽑은 쇠 울타리(흰 배경 시트 두 장)를 지금 맵 쇠 울타리 그림 자리에 PPU 32 점 밀도, 64색으로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/fence_import.py
import os
import shutil
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import pixel_ppu32 as pp
import prop_import as pi

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "fences_v1")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "fences_v0_map")

# 생성 그림, 자를 칸(원본 px: 왼, 위, 오른, 아래) → 덮어쓸 맵 그림.
# 가로, 세로는 맵에서 끝과 끝을 이어 붙여 놓아서, 대각은 6조각으로 잘라 조각마다 바닥 기준점을 잡아 둬서
# 예전 그림 상자에 꼭 맞게 채운다(대각은 새 그림이 약 49도로 눕혀져 가로만 줄여 예전 약 56도 기울기에 맞춘다).
# 기둥은 따로 세워 두는 그림이라 높이만 맞추고 폭은 새 그림 비율을 따른다(예전 폭에 맞추면 25% 홀쭉해진다).
TARGETS = [
    ("쇠 울타리 시트.webp", (40, 230, 600, 480), "쇠 울타리 가로1.png", "fit"),
    ("쇠 울타리 시트.webp", (700, 40, 840, 540), "쇠 울타리 세로1.png", "fit"),
    ("쇠 울타리 시트.webp", (1020, 660, 1160, 920), "쇠 울타리 기둥1.png", "height"),
    ("쇠 울타리 대각.webp", (180, 90, 740, 860), "쇠 울타리 대각1.png", "fit"),
    ("쇠 울타리 대각.webp", (800, 90, 1360, 860), "쇠 울타리 대각2.png", "fit"),
]

# 돌 기둥의 밝은 면(넓이 120~210px)이 창살 사이 빈칸(1300px 이상)과 함께 배경으로 지워져 구멍이 났다. 빈칸만 지우도록 기준을 올린다.
HOLE_MIN_AREA = 1000
# 창살, 가로대는 줄이면 1~2점 굵기라 외곽선을 두르면 통째로 검게 덮인다. 이 굵기 이하 줄은 외곽선 전 색을 되살린다.
THIN = 2
# 대각은 가로를 줄여 창살이 1점이 되며 옅은 색이 섞여 끊겨 보였다. 얇은 줄 중 금장식(따뜻한 색)이 아닌 점은
# 생성 그림 창살 평균색(시트 가로 조각에서 잰 값)에 가장 가까운 팔레트 색 하나로 칠한다.
IRON_SRC = np.array([105, 125, 133]) / 255.0
WARM = 0.15  # 빨강 - 파랑이 이보다 크면 금장식으로 본다

# 대각은 원본을 약 6.7배로 줄여 창살(원본 10px 안팎)이 1점도 못 채우고 군데군데 사라졌다. 줄이기 전에 창살을 좌우로 넓혀 둔다.
THICKEN = {"쇠 울타리 대각1.png": 7, "쇠 울타리 대각2.png": 7}


def thicken_iron(rgba, width):
    rgb, a = rgba[..., :3], rgba[..., 3] > 0.5
    iron = a & (rgb[..., 2] > rgb[..., 0] + 10 / 255) & (rgb.max(-1) < 200 / 255) & (rgb.max(-1) > 60 / 255)
    grown = cv2.dilate(iron.astype(np.uint8), np.ones((1, width), np.uint8)) > 0
    add = grown & ~a
    out = rgba.copy()
    out[add, :3] = IRON_SRC
    out[add, 3] = 1.0
    return out


def runs(mask, axis):
    # 칸마다 그 칸이 속한 가로(axis=1) 또는 세로(axis=0) 연속 구간 길이
    m = mask if axis == 1 else mask.T
    out = np.zeros(m.shape, int)
    for y in range(m.shape[0]):
        x = 0
        while x < m.shape[1]:
            if not m[y, x]:
                x += 1
                continue
            e = x
            while e < m.shape[1] and m[y, e]:
                e += 1
            out[y, x:e] = e - x
            x = e
    return out if axis == 1 else out.T


def keep_thin(inked, before, iron):
    a = before[..., 3] > 0.5
    thin = a & ((runs(a, 1) <= THIN) | (runs(a, 0) <= THIN))
    out = inked.copy()
    out[thin] = before[thin]
    cold = thin & (before[..., 0] - before[..., 2] <= WARM)
    out[cold, :3] = iron
    return out


def main():
    pi.HOLE_MIN_AREA = HOLE_MIN_AREA
    os.makedirs(BACKUP, exist_ok=True)
    palette = pp.load_palette()
    _, colors, lab_colors = palette
    iron = colors[np.argmin(((lab_colors - pp.oklab(IRON_SRC[None])) ** 2).sum(-1))]
    for src_name, cell, dst_name, mode in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        old = Image.open(backup).convert("RGBA")
        x0, y0, x1, y1 = old.getbbox()
        bw, bh = x1 - x0, y1 - y0
        rgba = pi.load_white_keyed(os.path.join(SRC, src_name), cell, True)
        if dst_name in THICKEN:
            rgba = thicken_iron(rgba, THICKEN[dst_name])
        ow = bw if mode == "fit" else round(bh * rgba.shape[1] / rgba.shape[0])
        out = pp.shrink(rgba, ow, bh)
        out = pp.apply_palette(out, palette)
        out = pp.remove_islands(out)
        out = pi.merge_similar(out)
        out = keep_thin(pp.outline(out, "ink", deep=False), out, iron)
        canvas = np.zeros((old.height, old.width, 4))
        left = x0 + (bw - ow) // 2
        canvas[y0:y1, left:left + ow] = out
        Image.fromarray((canvas * 255).round().astype(np.uint8), "RGBA").save(dst)
        print(dst_name, "box", (x0, y0, bw, bh), "new w", ow, "left", left)


if __name__ == "__main__":
    main()
