# 새 건물 그림체로 다시 뽑은 풀 울타리 그림을 PPU 32, 64색으로 바꿔 맵 그림에 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/hedge_import.py
# 결과 크기와 기준점은 hedge_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 칸을 다시 잡는다.
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

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "hedges_v2")
OLD_BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "hedges_v1")
RESULT = os.path.join(pp.ROOT, "ArtSource", "hedge_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 지금 맵에서 차지하는 폭(유닛), 그림 바닥에서 기준점까지 높이(유닛).
# 울타리 조각은 맵에 촘촘히 이어 붙여 놓여 있고 충돌 상자도 따로 있어, 건물처럼 점을 2배로 키우면 1.2~1.6배 커져 길이 좁아진다.
# 그래서 다른 소품처럼 큰 원본을 PixelOE로 지금 폭(PPU 32)에 맞춰 줄인다. 높이는 새 그림 비율을 따른다.
# 높이(유닛)를 적으면 그 높이로 맞춘다. 네모 블록은 새 그림이 원래 블록(65x106px)보다 20% 높게 나와,
# 세로로 겹쳐 쌓은 벽의 모양이 바뀌지 않도록 원래 높이에 맞춘다(11/8 사용자 요청 "원본 높이만큼").
HEDGES = [
    ("네모 블록 높게.png", "풀 울타리1.png", 2.07, 1.21, 106 / 32),
    ("긴 울타리.webp", "풀 울타리3.png", 4.375, 0.175, None),
    ("짧은 울타리.webp", "풀 울타리8.png", 4.38, 0.706, None),
    ("아치.webp", "풀 아치1.png", 3.56, 0.51, None),
    ("대각선.webp", "풀 울타리 9.png", 3.14, 0.383, None),
    # 구역 4에서 긴 울타리 두 개를 살짝 꺾어 겹쳐 놓던 자리를 한 장으로 바꾼다(11/8 사용자 요청). 쌍 하나가 약 7.9유닛 길이다.
    ("긴 곡선 울타리.webp", "풀 울타리 긴 곡선.png", 7.92, 0.25, None),
]

# 사각 울타리 벽 9-slice 그림(hedge_wall_tiles.py)이 잎, 꽃 색을 뽑아 쓰는 원본 블록 그림
WALL_SRC = "네모 블록 벽.webp"


# 울타리 조각은 맵에서 서로 겹쳐 이어 붙여 벽을 만든다(네모 블록은 가로 약 0.2유닛, 세로로 크게 겹친다).
# 조각마다 짙은 테두리가 있으면 이음매마다 줄이 그어져서(11/8 사용자 지적) 모든 조각의 위, 좌우 테두리를 메운다.
SEAM_DEPTH = 4       # 위, 좌우 가장자리에서 이 px 안쪽까지만 테두리를 메운다


def strip_seam_rim(rgba):
    # 위, 좌우의 짙은 테두리를 근처 잎 색으로 메운다. 바닥 테두리는 땅에 붙어 보이도록 남긴다.
    stripped = pp.outline(rgba, "strip", deep=True)
    mask = rgba[..., 3] > 0.5
    h = mask.shape[0]
    near_bottom = np.zeros_like(mask)
    for k in range(1, SEAM_DEPTH + 1):
        below = np.zeros_like(mask)
        below[:h - k] = ~mask[k:]
        below[h - k:] = True
        near_bottom |= below
    out = stripped.copy()
    out[near_bottom & mask] = rgba[near_bottom & mask]
    return out


# 네모 블록은 사각 벽돌처럼 이어 붙이는 조각이라 가장자리가 곧아야 한다. 잎 모양 들쭉날쭉한 끝 1~2px에
# 빈칸과 짙은 점이 번갈아 남아 이음매가 점선으로 보였다(11/8 사용자 지적).
STRAIGHT = {"풀 울타리1.png"}
STRAIGHT_DEPTH = 3   # 좌우, 위 가장자리에서 이 px까지 본다(들쭉날쭉한 끝은 1~3px)
STRAIGHT_DARK = 0.20 # 띠 안에서 이보다 어두운 점(가장 어두운 초록 두 단계)은 테두리로 보고 채운다


def straighten(rgba):
    # 좌우, 위 STRAIGHT_DEPTH px 띠 안의 빈칸과 아주 짙은 점만 띠 바로 안쪽 칸의 색으로 채운다.
    # 띠 전체를 거울처럼 뒤집으면 위쪽 꽃이 위아래로 겹쳐 보였다. 바닥은 땅에 닿는 테두리라 그대로 둔다.
    out = rgba.copy()
    h, w = out.shape[:2]
    d = STRAIGHT_DEPTH

    def bad(px):
        return (px[..., 3] < 0.5) | (pp.lum(px[..., :3]) < STRAIGHT_DARK)

    def fill(src):
        # 채울 색이 꽃(밝은 흰색)이면 꽃이 줄처럼 늘어나 보여서 가운데 밝기의 잎 색을 대신 쓴다
        return src if pp.lum(src[:3]) < 0.5 else leaf

    m = out[..., 3] > 0.5
    greens = out[m][pp.lum(out[m][:, :3]) < 0.5]
    leaf = greens[np.argsort(pp.lum(greens[:, :3]))[len(greens) // 2]]
    for y in range(h):
        for x in list(range(d)) + list(range(w - d, w)):
            if bad(out[y, x]):
                out[y, x] = fill(out[y, d if x < d else w - 1 - d])
    for x in range(w):
        for y in range(d):
            if bad(out[y, x]) or pp.lum(out[y, x, :3]) >= 0.5:
                out[y, x] = fill(out[d, x])
    return out


def load_rgba(path):
    # 초록 배경과 투명 배경 모두 초록 위에 얹은 뒤 초록 키로 지운다
    src = Image.open(path).convert("RGBA")
    green = Image.new("RGBA", src.size, (0, 255, 0, 255))
    green.alpha_composite(src)
    rgb = np.asarray(green.convert("RGB")).astype(float)
    # 건물용 초록 키는 밝은 잎까지 배경으로 보고 구멍을 냈다(긴 울타리, 대각선). 거의 순수한 #00FF00이면서 테두리에 이어진 칸만 배경으로 본다.
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    key = ((g > 200) & (r < 70) & (b < 70)).astype(np.uint8)
    n, labels = cv2.connectedComponents(key, connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    alpha = ~np.isin(labels, border[border > 0])
    ys, xs = np.nonzero(alpha)
    rgb = rgb[ys.min():ys.max() + 1, xs.min():xs.max() + 1] / 255.0
    alpha = alpha[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return np.dstack([rgb, alpha.astype(float)])


def main():
    os.makedirs(OLD_BACKUP, exist_ok=True)
    palette = pp.load_palette()
    result = {}
    for src_name, dst_name, width_units, pivot_units, height_units in HEDGES:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(OLD_BACKUP, dst_name)
        if os.path.exists(dst) and not os.path.exists(backup):
            shutil.copy2(dst, backup)
        rgba = load_rgba(os.path.join(SRC, src_name))
        ow = round(width_units * pp.TARGET)
        oh = round(height_units * pp.TARGET) if height_units else round(ow * rgba.shape[0] / rgba.shape[1])
        out = pp.shrink(rgba, ow, oh)
        out = pp.apply_palette(out, palette)
        # 풀 울타리는 검은 외곽선을 두르지 않는다(11/8 사용자 지적). 생성 그림의 짙은 초록 테두리를 그대로 쓴다.
        out = pp.remove_islands(out)
        out = strip_seam_rim(out)
        if dst_name in STRAIGHT:
            out = straighten(out)
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        result[dst_name] = {"size": [ow, oh], "pivot": [ow // 2, round(pivot_units * pp.TARGET)]}
        print(dst_name, (ow, oh), "pivot", result[dst_name]["pivot"])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
