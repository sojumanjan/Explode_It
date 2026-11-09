# 생성 그림을 "원본 해상도에서 색 먼저 나누고 → 출력 점마다 가장 많은 색 하나"로 줄여 중간 색 없이 픽셀화한다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/grid_pixelize.py
# 예전 방식(PixelOE 4x4 칸 고르기 + 64색 팔레트를 점마다)은 원본 점과 출력 격자가 안 맞아 선 하나에 갈색 6~7가지가 섞였다(11/9 사용자 지적).
# 생성 그림은 가짜 점 격자가 없는 매끈한 그림이라(선술집3 실루엣 계단 대부분 1~2px) 격자를 재는 대신,
# 원본 해상도에서 그림마다 COLORS개로 색을 나눈 뒤 출력 점 칸 안의 원본 칸 다수결로 고른다. 평균을 내지 않아 선은 선색, 면은 면색 하나만 남는다.
# 결과는 "_grid" 붙은 새 파일로 쓰고 원래 그림은 그대로 둔다. Unity에서 예전 그림의 가져오기 설정과 스프라이트 칸을 복사해 바꿔 끼운다.
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import fence_import as fi
import pixel_ppu32 as pp
import prop_import as pi

SRC = os.path.join(pp.ROOT, "ArtSource", "Original")
# 단일 그림: 생성 그림, 출력 파일, 출력 크기(지금 맵 그림과 같게 해 PPU, 기준점, 배치를 그대로 쓴다), 갇힌 흰 배경 지우기, 색 수, 채도 무게
# 선술집3도 시험했으나 사용자가 비교 후 이전 그림(손본 것)을 골라 뺐다(11/9)
TARGETS = [
    ("buildings_v4/선술집1.webp", "선술집1_grid.png", (305, 295), False, 20, 1.0),
    # 분수대 물줄기는 흰색이 그림의 일부라 갇힌 흰 배경을 지우지 않는다(prop_import와 같음).
    # 금색 백합 무늬가 넓이가 작아 20색에서는 돌 베이지에 묶여 사라졌다(채도 무게 2배로도 테두리만 남음). 24색, 채도 무게 3배로 따로 남긴다.
    ("props_v2/분수대.webp", "분수대1_grid.png", (305, 284), False, 24, 3.0),
]
SAMPLE = 60000   # 색 나누기에 쓸 표본 칸 수
SPECK_MAX = 2    # 다수결 뒤에도 남는 이 점 수 이하 조각은 둘레 색으로 메운다
ALPHA_MIN = 0.5  # 출력 칸 안에 그림이 이 비율 이상일 때만 점을 찍는다(실루엣이 원본과 같은 굵기)
# 쇠 울타리 창살은 출력 한 점보다 가늘어 절반 기준이면 군데군데 사라진다. 1/3만 덮여도 찍는다.
FENCE_ALPHA_MIN = 0.34
FENCE_COLORS = 16


def quantize(rgba, colors, chroma_w=1.0):
    m = rgba[..., 3] > 0.5
    lab = pp.oklab(rgba[..., :3][m]) * np.array([1.0, chroma_w, chroma_w])
    rng = np.random.default_rng(0)
    pick = lab[rng.choice(len(lab), min(SAMPLE, len(lab)), replace=False)]
    centers = pp.kmeans(pick, colors)
    label = np.full(m.shape, -1)
    idx = np.empty(len(lab), int)
    for s in range(0, len(lab), 200000):
        idx[s:s + 200000] = np.argmin(((lab[s:s + 200000, None] - centers[None]) ** 2).sum(-1), 1)
    label[m] = idx
    return label, np.clip(pp.from_oklab(centers / np.array([1.0, chroma_w, chroma_w])), 0, 1)


def mode_downsample(label, ow, oh, alpha_min):
    h, w = label.shape
    xs = np.linspace(0, w, ow + 1).round().astype(int)
    ys = np.linspace(0, h, oh + 1).round().astype(int)
    out = np.full((oh, ow), -1)
    for j in range(oh):
        band = label[ys[j]:ys[j + 1]]
        for i in range(ow):
            cell = band[:, xs[i]:xs[i + 1]].ravel()
            opaque = cell[cell >= 0]
            if len(cell) and len(opaque) >= alpha_min * len(cell):
                out[j, i] = np.bincount(opaque).argmax()
    return out


def drop_specks(label):
    out = label.copy()
    for k in np.unique(label[label >= 0]):
        n, lab, stats, _ = cv2.connectedComponentsWithStats((label == k).astype(np.uint8), connectivity=8)
        for i in range(1, n):
            if stats[i, cv2.CC_STAT_AREA] > SPECK_MAX:
                continue
            blob = lab == i
            ring = (cv2.dilate(blob.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0) & ~blob
            around = label[ring]
            around = around[around >= 0]
            if len(around):
                out[blob] = np.bincount(around).argmax()
    return out


def pixelize(rgba, ow, oh, colors, alpha_min, chroma_w=1.0):
    label, palette = quantize(rgba, colors, chroma_w)
    small = drop_specks(mode_downsample(label, ow, oh, alpha_min))
    out = np.zeros((oh, ow, 4))
    m = small >= 0
    out[m, :3] = palette[small[m]]
    out[m, 3] = 1
    return pp.remove_islands(out)


# 변환 뒤 손으로 고친 그림. 다시 돌려도 덮어쓰지 않는다.
# 가로1: 사용자가 가운데 창살 끝을 다듬고, 그 모양을 나머지 6개 창살 끝(y 55~72)에 그대로 복사함(11/9)
# 대각1: 사용자가 왼쪽 3번째 창살 끝을 다듬고 꼭짓점을 맞춰 나머지 5개에 복사, 이후 사용자가 마저 다듬음(11/9)
# 대각2: 대각1을 그대로 좌우반전한 그림(175x175, PPU 35). 조각 칸도 대각1 조각을 뒤집어 넣음(대각2 조각 k = 대각1 조각 5-k)
# 세로1: 사용자가 맨 아래 장식(88~95줄)을 다듬고, 9줄 간격으로 위에 5개 복사해 모두 6개(7개는 빽빽해 사용자가 되돌림)(11/9)
HAND_EDITED = {"쇠 울타리 가로1_grid.png", "쇠 울타리 대각1_grid.png", "쇠 울타리 대각2_grid.png", "쇠 울타리 세로1_grid.png"}


def save(out, dst_name):
    if dst_name in HAND_EDITED:
        print("skip (손으로 고친 그림):", dst_name)
        return
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(os.path.join(bi.PROPS, dst_name))
    used = np.unique((out[out[..., 3] > 0.5][:, :3] * 255).round(), axis=0)
    print(dst_name, out.shape[1::-1], "colors", len(used))


def fence_outline(out):
    # 다른 맵 그림과 같은 검은 1px 실루엣 외곽선. 창살, 가로대처럼 2점 이하 굵기 줄은 통째로 검게 덮이므로 원래 색을 되살린다
    inked = pp.outline(out, "ink", deep=False)
    a = out[..., 3] > 0.5
    thin = a & ((fi.runs(a, 1) <= fi.THIN) | (fi.runs(a, 0) <= fi.THIN))
    inked[thin] = out[thin]
    return inked


def main():
    for src_name, dst_name, (ow, oh), holes, colors, chroma_w in TARGETS:
        rgba = pi.load_white_keyed(os.path.join(SRC, src_name), None, holes)
        save(pp.outline(pixelize(rgba, ow, oh, colors, ALPHA_MIN, chroma_w), "ink", deep=False), dst_name)
    # 쇠 울타리: fence_import와 같은 자르기, 같은 캔버스, 같은 자리에 그려 대각 6조각 칸과 기준점을 그대로 쓴다
    pi.HOLE_MIN_AREA = fi.HOLE_MIN_AREA
    for src_name, cell, dst_name, mode in fi.TARGETS:
        old = Image.open(os.path.join(fi.BACKUP, dst_name)).convert("RGBA")
        x0, y0, x1, y1 = old.getbbox()
        bw, bh = x1 - x0, y1 - y0
        rgba = pi.load_white_keyed(os.path.join(fi.SRC, src_name), cell, True)
        if dst_name in fi.THICKEN:
            rgba = fi.thicken_iron(rgba, fi.THICKEN[dst_name])
        ow = bw if mode == "fit" else round(bh * rgba.shape[1] / rgba.shape[0])
        out = fence_outline(pixelize(rgba, ow, bh, FENCE_COLORS, FENCE_ALPHA_MIN))
        canvas = np.zeros((old.height, old.width, 4))
        left = x0 + (bw - ow) // 2
        canvas[y0:y1, left:left + ow] = out
        save(canvas, dst_name.replace(".png", "_grid.png"))


if __name__ == "__main__":
    main()
