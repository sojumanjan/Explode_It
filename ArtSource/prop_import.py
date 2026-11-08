# 마차 그림체로 다시 뽑은 맵 소품(흰 배경)을 지금 맵 소품 자리와 크기에 맞춰 PPU 32 점 밀도, 64색으로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/prop_import.py
# 결과(그림 크기, 기준점)는 prop_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 칸을 다시 잡는다.
import json
import os
import shutil
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "props_v2")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "props_v1")
RESULT = os.path.join(pp.ROOT, "ArtSource", "prop_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 지금 스프라이트 칸(텍스처 왼쪽 아래 기준 x, y, w, h), 기준점(칸 기준 px), 맵 점 밀도 배수, 좌우 반전.
# 세 소품 모두 맵에서 PPU / 배율이 32라(점 밀도가 다른 소품과 같다) 지금 PPU와 배율을 그대로 두고 그림만 바꾼다.
# 밀도 배수 = (PPU / 배율) / 32. 예전 그림 점 수를 이 값으로 나누면 PPU 32 기준 점 수가 된다.
TARGETS = [
    ("마차.webp", "마차_hd.png", (0, 0, 157, 138), (78.03, 61.82), 31.96872 / 32, False),
    ("마차.webp", "마차2_hd.png", (0, 0, 157, 138), (78.03, 16.83), 31.96872 / 32, True),
    ("나무.webp", "나무1.png", (0, 0, 177, 177), (87.55, 6.91), 17.28516 / 0.54 / 32, False),
    ("테이블 의자세트.webp", "테이블 의자세트1.png", (2, 5, 84, 71), (42.0, 8.74), 42.70834 / 1.33 / 32, False),
    ("분수대.webp", "분수대1_hd.png", (0, 0, 298, 295), (148.68, 36.16), 31.9965 / 32, False),
    # 아래는 한 장에 3x2로 뽑은 그림(소품 6종.webp)에서 칸을 잘라 쓴다(SHEET_CELLS).
    # 같은 그림을 배율을 달리해 여러 곳에 놓은 소품은 가장 많이 놓인 배율로 밀도를 맞춘다.
    ("소품 6종.webp", "보급 상자1.png", (0, 0, 96, 96), (49.31, 12.84), 9.375 / 0.29 / 32, False),
    ("소품 6종.webp", "무기 거치대1.png", (0, 0, 105, 105), (52.24, 6.25), 10.25391 / 0.32 / 32, False),
    ("소품 6종.webp", "무기 거치대2.png", (12, 6, 82, 96), (41.0, 48.0), 10.25391 / 0.32 / 32, True),
    ("소품 6종.webp", "선술집 조형물1.png", (8, 6, 86, 88), (43.0, 10.87), 20 / 0.63 / 32, False),
    ("소품 6종.webp", "나무 상자1.png", (3, 2, 44, 44), (22.0, 5.5), 34.24657 / 1.08 / 32, False),
    ("소품 6종.webp", "나무 벤치1.png", (15, 7, 65, 51), (32.5, 6.36), 15.52288 / 0.48 / 32, False),
    ("소품 6종.webp", "3단 오크통1.png", (2, 2, 73, 76), (36.5, 9.39), 42.07921 / 1.32 / 32, False),
]
# 한 장 그림에서 소품마다 자를 칸(원본 px: 왼, 위, 오른, 아래)
SHEET_CELLS = {
    "보급 상자1.png": (0, 0, 330, 560),
    "무기 거치대1.png": (330, 0, 700, 560),
    "무기 거치대2.png": (330, 0, 700, 560),
    "선술집 조형물1.png": (700, 0, 1024, 560),
    "나무 상자1.png": (0, 560, 330, 1024),
    "나무 벤치1.png": (330, 560, 700, 1024),
    "3단 오크통1.png": (700, 560, 1024, 1024),
}
# 64색 팔레트에 밝은 연두가 없어 나무 잎이 노랗게, 줄기가 잿빛으로 바뀌었다. 나무는 생성 그림 색을 그대로 쓴다.
NO_PALETTE = {"나무1.png"}
EDGE_ERODE = 2       # 흰 배경과 섞인 가장자리 점(원본 해상도 px). 생성 그림의 외곽선은 한 점이 7~8px라 깎아도 남는다
HOLE_WHITE = 235     # 바퀴살, 테이블 다리 사이처럼 그림 안에 갇힌 흰 배경. 이보다 밝고 무채색이면 지운다(크림색 천막은 남는다)
HOLE_CHROMA = 15
HOLE_MIN_AREA = 120  # 이 넓이(원본 px) 이상인 갇힌 흰 덩어리만 배경으로 본다
# 갇힌 흰 배경을 지울 소품. 분수대 물줄기, 선술집 조형물 책장은 흰색이 그림의 일부라 지우면 구멍이 난다.
OPEN_HOLES = {"마차_hd.png", "마차2_hd.png", "테이블 의자세트1.png", "무기 거치대1.png", "무기 거치대2.png", "나무 벤치1.png"}


def load_white_keyed(path, cell=None, open_holes=True):
    src = Image.open(path).convert("RGBA")
    if cell:
        src = src.crop(cell)
    white = Image.new("RGBA", src.size, (255, 255, 255, 255))
    white.alpha_composite(src)
    rgb = np.asarray(white.convert("RGB")).astype(float)
    chroma = rgb.max(-1) - rgb.min(-1)
    bright = (rgb.min(-1) > 200) & (chroma < 40)
    n, labels = cv2.connectedComponents(bright.astype(np.uint8), connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    fg = ~np.isin(labels, border[border > 0])
    if open_holes:
        # 칼날 밝은 면도 흰색에 가까워 통째로 지우면 거치대 칼에 구멍이 났다(11/8 사용자 지적).
        # 배경 빈칸(벤치 등받이, 창살 사이 141px 이상)만 지우고 작은 흰 조각(칼날 빛, 99px 이하)은 남긴다.
        hole = ((rgb.min(-1) > HOLE_WHITE) & (chroma < HOLE_CHROMA)).astype(np.uint8)
        n, labels, stats, _ = cv2.connectedComponentsWithStats(hole, connectivity=4)
        big = np.zeros(n, bool)
        big[1:] = stats[1:, cv2.CC_STAT_AREA] >= HOLE_MIN_AREA
        fg &= ~big[labels]
    n, labels, stats, _ = cv2.connectedComponentsWithStats(fg.astype(np.uint8), connectivity=4)
    keep = np.zeros_like(fg)
    big = stats[1:, cv2.CC_STAT_AREA].max()
    for i in range(1, n):
        # 촛불처럼 본체와 떨어진 조각도 어느 정도 크면 남긴다
        if stats[i, cv2.CC_STAT_AREA] >= big * 0.01:
            keep |= labels == i
    fg = cv2.erode(keep.astype(np.uint8), np.ones((3, 3), np.uint8), iterations=EDGE_ERODE) > 0
    ys, xs = np.nonzero(fg)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    return np.dstack([rgb[y0:y1, x0:x1] / 255.0, fg[y0:y1, x0:x1].astype(float)])


# 칼날, 창끝은 줄이면서 얇은 테두리가 사라져 나무 창살과 섞여 보였다(11/8 사용자 지적). 밝은 무채색 쇠 조각 둘레에 검은 줄을 긋는다.
INK_METAL = {"무기 거치대1.png", "무기 거치대2.png"}
# 칼날은 크림빛이 도는 회색이라 무채색 기준으로는 밝은 면 일부만 잡혀 칼날 가운데에 줄이 그어졌다.
# 채도로 나눈다: 칼날, 창끝(채도 낮음)과 나무(채도 높은 주황).
METAL_LUM = 0.40
METAL_SAT = 0.30
WOOD_SAT = 0.35
METAL_MIN_AREA = 8


def saturation(rgb):
    mx, mn = rgb.max(-1), rgb.min(-1)
    return np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)


def ink_metal(rgba):
    # 쇠와 맞닿은 나무 점에만 줄을 긋는다. 방패 안 흰 별과 파란 면 사이처럼 나무가 아닌 곳은 건드리지 않고, 칼날 폭도 줄이지 않는다.
    rgb, mask = rgba[..., :3], rgba[..., 3] > 0.5
    sat = saturation(rgb)
    metal = mask & (pp.lum(rgb) > METAL_LUM) & (sat < METAL_SAT)
    metal = cv2.morphologyEx(metal.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8)) > 0
    # 나무 결의 밝은 점(1~3px)도 채도가 낮아 쇠로 잡혀 점 옆에 줄이 묻었다. 칼날, 창끝 크기 조각만 남긴다.
    n, labels, stats, _ = cv2.connectedComponentsWithStats(metal.astype(np.uint8), connectivity=4)
    big = np.zeros(n, bool)
    big[1:] = stats[1:, cv2.CC_STAT_AREA] >= METAL_MIN_AREA
    metal = big[labels]
    wood = mask & ~metal & (sat > WOOD_SAT) & (rgb[..., 0] > rgb[..., 2])
    pad = np.pad(metal, 1)
    near = pad[2:, 1:-1] | pad[:-2, 1:-1] | pad[1:-1, 2:] | pad[1:-1, :-2]
    out = rgba.copy()
    out[near & wood, :3] = pp.INK
    return out


# 분수대는 줄이면서 물 가장자리와 돌 테두리에 갈색, 짙은 점이 한 칸씩 흩어져 외곽선 근처가 자글자글해 보였다(11/9 사용자 지적).
# 이웃 8칸 중 같은 색이 하나도 없는 외톨이 점을 이웃에서 가장 많은 색으로 바꾼다. 줄과 모양은 이웃에 같은 색이 있어 남는다.
# 바깥과 맞닿은 가장자리 칸(그림 이웃 5칸 미만)은 실루엣이 바뀌지 않게 건드리지 않는다.
DESPECKLE = {"분수대1_hd.png"}
DESPECKLE_PASSES = 2


def despeckle(rgba):
    out = rgba.copy()
    h, w = out.shape[:2]
    for _ in range(DESPECKLE_PASSES):
        q = (out[..., :3] * 255).round().astype(np.int64)
        key = q[..., 0] * 65536 + q[..., 1] * 256 + q[..., 2]
        a = out[..., 3] > 0.5
        kp, ap = np.pad(key, 1, constant_values=-1), np.pad(a, 1)
        same = np.zeros((h, w), int)
        nbrs = []
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy == 0 and dx == 0:
                    continue
                nk, na = kp[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx], ap[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx]
                same += (nk == key) & na
                nbrs.append(np.where(na, nk, -1))
        nbrs = np.stack(nbrs, -1)
        for y, x in zip(*np.nonzero(a & (same == 0))):
            v = nbrs[y, x]
            v = v[v >= 0]
            if len(v) < 5:
                continue
            vals, cnt = np.unique(v, return_counts=True)
            m = vals[np.argmax(cnt)]
            out[y, x, :3] = np.array([m // 65536, (m // 256) % 256, m % 256]) / 255.0
    return out


# 줄인 소품은 비슷한 색이 한 칸씩 번갈아 찍혀 잘게 부서져 보이고 매끄럽지 않아 건물, 울타리와 안 어울렸다(11/9 사용자 지적).
# 칸마다 3x3 이웃에서 가장 많은 색이 자기 색과 비슷하면(RGB 차이 합 MERGE_THRESH 이하) 그 색으로 바꿔, 비슷한 색을 덩어리로 모은다.
# 차이가 큰 색(테두리, 쇠 띠, 별 무늬)은 바뀌지 않아 모양과 경계는 남는다. 이웃이 다 그림인 칸만 바꿔 실루엣도 그대로다.
MERGE_THRESH = 100
MERGE_PASSES = 3
MERGE_MIN_COUNT = 4  # 이웃 9칸 중 이 수 이상 같은 색이 모여 있을 때만 그 색을 따른다


def merge_similar(rgba):
    out = rgba.copy()
    h, w = out.shape[:2]
    for _ in range(MERGE_PASSES):
        q = (out[..., :3] * 255).round().astype(np.int64)
        key = q[..., 0] * 65536 + q[..., 1] * 256 + q[..., 2]
        a = out[..., 3] > 0.5
        kp, ap = np.pad(key, 1, constant_values=-1), np.pad(a, 1)
        st = np.stack([np.where(ap[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx], kp[1 + dy:h + 1 + dy, 1 + dx:w + 1 + dx], -1)
                       for dy in (-1, 0, 1) for dx in (-1, 0, 1)], -1)
        new = out.copy()
        for y, x in zip(*np.nonzero(a)):
            v = st[y, x]
            if (v < 0).any():
                continue
            vals, cnt = np.unique(v, return_counts=True)
            m = vals[np.argmax(cnt)]
            if m == key[y, x] or cnt.max() < MERGE_MIN_COUNT:
                continue
            mc = np.array([m // 65536, (m // 256) % 256, m % 256])
            if np.abs(mc - q[y, x]).sum() <= MERGE_THRESH:
                new[y, x, :3] = mc / 255.0
        out = new
    return out


def old_box(path, rect):
    a = np.asarray(Image.open(path).convert("RGBA"))[..., 3] > 127
    th = a.shape[0]
    x, y, w, h = rect
    sub = a[th - y - h:th - y, x:x + w]
    ys, xs = np.nonzero(sub)
    return xs.min(), h - 1 - ys.max(), xs.max() + 1, h - ys.min()


def main():
    os.makedirs(BACKUP, exist_ok=True)
    palette = pp.load_palette()
    result = {}
    for src_name, dst_name, rect, pivot, density, flip in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        bx0, by0, bx1, by1 = old_box(backup, rect)
        bw, bh = bx1 - bx0, by1 - by0
        rgba = load_white_keyed(os.path.join(SRC, src_name), SHEET_CELLS.get(dst_name), dst_name in OPEN_HOLES)
        # 새 그림은 가로세로 비율이 예전과 달라(나무는 더 둥글다) 늘리지 않고, 차지하는 넓이가 예전과 같도록 비율을 지켜 줄인다
        k = np.sqrt(bw * bh / (rgba.shape[1] * rgba.shape[0])) / density
        ow, oh = round(rgba.shape[1] * k), round(rgba.shape[0] * k)
        out = pp.shrink(rgba, ow, oh)
        if dst_name not in NO_PALETTE:
            out = pp.apply_palette(out, palette)
        out = pp.remove_islands(out)
        # 줄이면서 생성 그림의 갈색 외곽선이 군데군데 끊기거나 안으로 파고들어(11/8 사용자 지적), 건물처럼 검은 1px 외곽선을 새로 두른다.
        # deep(안쪽 짙은 겹 지우기)은 작은 소품에서 나무 다리의 그늘을 밝은 줄로, 통 테두리를 찌그러지게 바꿔서(11/8 사용자 지적) 끈다.
        if dst_name in DESPECKLE:
            out = despeckle(out)
        if dst_name not in NO_PALETTE:
            out = merge_similar(out)
        out = pp.outline(out, "ink", deep=False)
        if dst_name in INK_METAL:
            out = ink_metal(out)
        if flip:
            out = out[:, ::-1]
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        # 기준점은 예전 그림 상자 안 비율 자리(가로 가운데, 바닥에서 높이 비율)를 그대로 옮긴다
        # 마차2의 예전 그림도 이미 뒤집힌 그림이라 비율 자리를 그대로 쓴다
        fx = (pivot[0] - bx0) / bw
        piv = [fx * ow, max(0.0, (pivot[1] - by0) / bh * oh)]
        result[dst_name] = {"size": [ow, oh], "pivot": [round(piv[0], 2), round(piv[1], 2)]}
        print(dst_name, (ow, oh), "old box", (bw, bh), "pivot", result[dst_name]["pivot"])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
