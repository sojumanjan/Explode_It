# 새로 뽑은 단순한 건물 그림(초록 배경)을 픽셀 격자로 되살려 PPU 32 맵 건물 그림으로 바꿔 넣는다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/building_import.py
import json
import os
import shutil
import sys

import numpy as np
from PIL import Image
import cv2

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import icon_sheet_import as isi
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "buildings_v3")
OLD_BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "buildings_v1")
PROPS = os.path.join(pp.ART, "Props")
RESULT = os.path.join(pp.ROOT, "ArtSource", "building_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림. 같은 파일에 덮어써서 씬 참조(GUID)를 그대로 둔다.
BUILDINGS = {
    "선술집1.webp": "선술집1_hd.png",
    "선술집2.webp": "선술집2_hd.png",
    "선술집3.webp": "선술집3.png",
    "기사 훈련 건물.webp": "기사 훈련 건물 2_hd.png",
    "파란 원형 성.webp": "파란 원형 성1_hd.png",
    "풍차.webp": "풍차1_hd.png",
}
# 그림 한 점을 게임 몇 칸(PPU 32 픽셀)으로 키울지. 건물마다 다르게 맞추면 점 크기가 건물마다 달라져서 모두 같은 정수로 둔다.
# v2(3배)는 점이 너무 굵어(사용자 지적) 점을 더 잘게 다시 뽑은 v3를 2배로 넣는다. 2배면 맵 건물 폭과 ±10% 안으로 맞는다.
BLOCK = 2
MIN_PIECE = 6        # 본체와 떨어진 조각(깃발, 연기)도 이 칸 수 이상이면 남긴다
MAX_HOLE = 6         # 그림 안에 갇힌 빈칸이 이 칸 수 이하면 지워진 점으로 보고 채운다


def is_key(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (g > 150) & (g > r + 60) & (g > b + 60)


def coherence(img, p, axis):
    # 이 간격으로 픽셀 경계가 얼마나 고르게 모이는지(0~1)와 그 위상
    g = np.abs(np.diff(img, axis=1 - axis)).sum(-1)
    ys, xs = np.nonzero(g > isi.EDGE)
    pos = (xs if axis == 0 else ys) + 0.5
    w = g[ys, xs]
    z = (w * np.exp(2j * np.pi * pos / p)).sum() / w.sum()
    return abs(z), (np.angle(z) / (2 * np.pi) * p) % p


def is_white_background(rgb):
    # 흰 배경으로 뽑힌 그림: 테두리에 이어진 흰 칸만 배경이다. 흰 꽃처럼 그림 안에 갇힌 흰색은 남긴다.
    white = (rgb.min(-1) > 225).astype(np.uint8)
    n, labels = cv2.connectedComponents(white, connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    return np.isin(labels, border[border > 0])


def background_of(rgb, background):
    return is_white_background(rgb) if background == "white" else is_key(rgb)


def cell_colors(img, px, py, ox, oy, background, strip_green_fringe):
    # 칸마다 자기 칸 안의 원본 점만 본다. 그림 칸(점 약 4px)보다 넓은 7x7 중앙값을 쓰면 가장자리 칸이 초록 배경과 섞여
    # 줄마다 남거나 지워져 벽이 울퉁불퉁했다(11/8 파란 원형 성, 기사 훈련 건물 왼쪽 벽).
    # 그래서 칸의 절반 이상이 그림이면 남기고, 색은 그 칸의 그림 점(배경, 초록 번짐 제외)만으로 정한다.
    bg = background_of(img, background)
    if strip_green_fringe:
        r, g, b = img[..., 0], img[..., 1], img[..., 2]
        bg = bg | ((g > r + 20) & (g > b + 20))
    h, w = img.shape[:2]
    xs = np.arange(ox - px, w, px)
    ys = np.arange(oy - py, h, py)
    xs, ys = xs[xs + px > 0], ys[ys + py > 0]
    out = np.zeros((len(ys), len(xs), 3))
    cover = np.zeros((len(ys), len(xs)))
    for j, y in enumerate(ys):
        y0, y1 = int(round(max(0, y))), int(round(min(h, y + py)))
        for i, x in enumerate(xs):
            x0, x1 = int(round(max(0, x))), int(round(min(w, x + px)))
            pix = img[y0:y1, x0:x1].reshape(-1, 3)
            fg = ~bg[y0:y1, x0:x1].reshape(-1)
            if pix.size == 0:
                continue
            cover[j, i] = fg.mean()
            out[j, i] = np.median(pix[fg] if fg.any() else pix, 0)
    mask = cover >= 0.5
    # 그림 안에 갇힌 구멍(깃발 문양의 청록 눈처럼 초록빛이라 지워진 칸)은 다시 채운다. 바깥과 이어진 빈칸만 배경이다.
    n, labels = cv2.connectedComponents((~mask).astype(np.uint8), connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    holes = ~mask & ~np.isin(labels, border)
    # 풍차 날개 사이처럼 일부러 뚫린 큰 빈칸은 그대로 두고, 몇 칸짜리 작은 구멍만 채운다
    sizes = np.bincount(labels.ravel())
    holes &= sizes[labels] <= MAX_HOLE
    for j, i in zip(*np.nonzero(holes)):
        y0, y1 = int(round(max(0, ys[j]))), int(round(min(h, ys[j] + py)))
        x0, x1 = int(round(max(0, xs[i]))), int(round(min(w, xs[i] + px)))
        out[j, i] = np.median(img[y0:y1, x0:x1].reshape(-1, 3), 0)
    return out, mask | holes


def convert(src_path, background="green", strip_green_fringe=True):
    # strip_green_fringe: 초록 배경과 섞인 초록빛 테두리 칸까지 지운다. 초록인 풀 울타리에는 끈다.
    src = Image.open(src_path).convert("RGBA")
    if np.asarray(src)[..., 3].min() < 255:
        # 투명 배경으로 뽑힌 그림(풀 울타리 네모 블록, 아치)은 초록 위에 얹어 초록 배경 그림과 똑같이 처리한다
        green = Image.new("RGBA", src.size, (0, 255, 0, 255))
        green.alpha_composite(src)
        src, background = green, "green"
    img = np.asarray(src.convert("RGB")).astype(float)
    ys, xs = np.nonzero(~background_of(img, background))
    pad = 16
    y0, y1 = max(0, ys.min() - pad), min(img.shape[0], ys.max() + pad + 1)
    x0, x1 = max(0, xs.min() - pad), min(img.shape[1], xs.max() + pad + 1)
    crop = img[y0:y1, x0:x1]
    px, py, ox, oy = isi.pixel_period(crop, ((3.0, 14.0), (3.0, 14.0)))
    if abs(px - py) / min(px, py) > 0.04:
        # 가로세로 간격이 다르게 잡히면 한쪽이 엉뚱한 배수를 잡은 것이라 그림이 납작해졌다(기사 훈련 건물, 선술집 2).
        # 생성 그림의 점은 정사각형이므로, 두 후보 중 두 축 모두에서 경계가 더 잘 맞는 간격 하나를 고른다.
        best = max((px, py), key=lambda p: coherence(crop, p, 0)[0] + coherence(crop, p, 1)[0])
        px = py = best
        ox = coherence(crop, best, 0)[1]
        oy = coherence(crop, best, 1)[1]
    cells, mask = cell_colors(crop, px, py, ox, oy, background, strip_green_fringe)
    n, labels, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), connectivity=4)
    keep = np.zeros_like(mask)
    for i in range(1, n):
        if stats[i, cv2.CC_STAT_AREA] >= MIN_PIECE:
            keep |= labels == i
    mask = keep

    # 다른 맵 소품과 색을 맞추려고 소품과 같은 64색 팔레트(채도 맞추기 포함)를 입힌다
    rgba_cells = np.dstack([cells / 255.0, mask.astype(float)])
    rgb = pp.apply_palette(rgba_cells, pp.load_palette())[..., :3] * mask[..., None]

    ys, xs = np.nonzero(mask)
    rgb = rgb[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    alpha = mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    rgba = np.dstack([rgb, alpha.astype(float)])
    rgba = rgba.repeat(BLOCK, 0).repeat(BLOCK, 1)
    # 생성 그림의 갈색 테두리만으로는 맵 위에서 흐릿해 보여(사용자 지적) 다른 소품처럼 검은 1px 외곽선을 두른다.
    # 키운 뒤에 칠해야 소품, 캐릭터와 같은 1px 굵기가 된다. deep은 안쪽 갈색 테두리 겹을 지워 줄이 두 겹으로 보이지 않게 한다.
    rgba = pp.outline(pp.remove_islands(rgba), "ink", deep=True)
    return rgba, (px, py)


def main():
    os.makedirs(OLD_BACKUP, exist_ok=True)
    result = {}
    for src_name, dst_name in BUILDINGS.items():
        dst = os.path.join(PROPS, dst_name)
        backup = os.path.join(OLD_BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        old = Image.open(backup)
        rgba, period = convert(os.path.join(SRC, src_name))
        out = Image.fromarray((rgba * 255).round().astype(np.uint8), "RGBA")
        out.save(dst)
        result[dst_name] = {
            "size": list(out.size),
            "old_size": list(old.size),
            "period": [round(period[0], 2), round(period[1], 2)],
        }
        print(dst_name, out.size, "old", old.size, "period %.2f %.2f" % period)
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
