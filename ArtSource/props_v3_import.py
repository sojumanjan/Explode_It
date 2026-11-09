# 새로 뽑은 소품 8종(깃발, 카펫, 꽃 조형물 2, 꽃 가로등, 촛대, 밀가루 포대, 미니 음수대, 11/9)을 지금 맵 소품 자리에 같은 키, 같은 기준점 자리로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/props_v3_import.py
# 결과(그림 크기, 기준점)는 props_v3_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 칸을 다시 잡는다. PPU는 그대로 둔다.
import json
import os
import shutil
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import grid_pixelize as gp
import pixel_ppu32 as pp
import prop_import as pi

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "props_v3")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "props_v3_oldmap")
RESULT = os.path.join(pp.ROOT, "ArtSource", "props_v3_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 지금 맵에서 쓰는 스프라이트 칸(텍스처 왼쪽 아래 기준 x, y, w, h), 기준점(칸 기준 px), 색 수, 채도 무게.
# 키는 예전 그림 상자와 같게, 폭은 새 그림 비율을 따른다. PPU와 맵 배율을 그대로 두므로 놓인 크기와 점 밀도가 예전과 같다.
# 보석, 금 문장처럼 넓이가 작은 색은 돌 크림색에 묻히지 않게 채도에 무게를 둔다(분수대, 왕 석상과 같음).
TARGETS = [
    ("빨간 깃발.webp", "빨간 깃발1.png", (27, 6, 50, 147), (25.0, 73.5), 12, 1.0),
    ("레드 카펫.webp", "레드 카펫 가로1.png", (5, 12, 304, 89), (152.0, 44.5), 8, 1.0),
    ("하얀 꽃 조형물.webp", "하얀 꽃 조형물1.png", (19, 3, 20, 54), (10.0, 6.66), 10, 1.0),
    ("빨간 꽃 조형물.webp", "빨간 꽃 조형물1.png", (11, 3, 45, 63), (22.5, 7.65), 12, 3.0),
    ("꽃 가로등.webp", "꽃 가로등1.png", (44, 3, 34, 118), (17.0, 14.32), 12, 3.0),
    ("긴 촛대.webp", "긴 촛대1.png", (21, 13, 32, 92), (16.0, 11.32), 10, 1.0),
    ("밀가루 포대.webp", "밀가루 포대1.png", (2, 1, 56, 55), (28.0, 6.5), 10, 1.0),
    ("미니 음수대.webp", "미니 음수대.png", (17, 6, 57, 81), (28.5, 9.81), 12, 3.0),
]
# 깃발은 가로대 고리 사이에 흰 배경이 갇힌다. 크림색 돌, 흰 꽃, 밀가루는 흰색에 가까워 다른 소품은 갇힌 흰색을 지우지 않는다.
OPEN_HOLES = {"빨간 깃발1.png": 120}
# 카펫은 길게 깔리는 소품이라 비율대로 줄이면 예전보다 짧아진다. 키에 맞춰 줄인 뒤 가운데 한 줄을 늘려 예전 길이로 맞춘다(모서리 문장은 그대로).
STRETCH_X = {"레드 카펫 가로1.png"}
# 촛대 기둥은 1~2점 굵기라 검은 외곽선에 통째로 덮여 검은 막대가 됐다. 쇠 울타리 창살처럼 가는 줄은 원래 색을 되살린다.
THIN_OUTLINE = {"긴 촛대1.png"}


def stretch_middle(out, width):
    w = out.shape[1]
    if width <= w:
        return out
    mid = w // 2
    fill = np.repeat(out[:, mid:mid + 1], width - w, axis=1)
    return np.concatenate([out[:, :mid], fill, out[:, mid:]], axis=1)


def main():
    os.makedirs(BACKUP, exist_ok=True)
    result = {}
    for src_name, dst_name, rect, pivot, colors, chroma_w in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        bx0, by0, bx1, by1 = pi.old_box(backup, rect)
        bw, bh = bx1 - bx0, by1 - by0
        pi.HOLE_MIN_AREA = OPEN_HOLES.get(dst_name, pi.HOLE_MIN_AREA)
        rgba = pi.load_white_keyed(os.path.join(SRC, src_name), None, dst_name in OPEN_HOLES)
        h = int(bh)
        w = round(h * rgba.shape[1] / rgba.shape[0])
        out = gp.pixelize(rgba, w, h, colors, gp.ALPHA_MIN, chroma_w)
        if dst_name in STRETCH_X:
            out = stretch_middle(out, int(bw))
            w = out.shape[1]
        out = gp.fence_outline(out) if dst_name in THIN_OUTLINE else pp.outline(out, "ink", deep=False)
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        # 기준점은 예전 그림 상자 안의 비율 자리를 그대로 옮긴다(발밑, 카펫은 가운데)
        piv = [float((pivot[0] - bx0) / bw * w), max(0.0, float((pivot[1] - by0) / bh * h))]
        result[dst_name] = {"size": [w, h], "pivot": [round(piv[0], 2), round(piv[1], 2)]}
        print(dst_name, (w, h), "old box", (int(bw), int(bh)), result[dst_name])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
