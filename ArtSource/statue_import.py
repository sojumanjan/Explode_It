# 정면으로 다시 뽑은 석상 3종(흰 배경, 11/9 v2)을 지금 맵 석상 자리에 같은 키, 같은 발밑 기준점, PPU 32 점 밀도로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/statue_import.py
# 결과(그림 크기, 기준점, PPU)는 statue_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 칸과 PPU를 다시 잡는다.
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

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "statues_v2")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "statues_v0_map")
RESULT = os.path.join(pp.ROOT, "ArtSource", "statue_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 처음 맵 그림의 스프라이트 칸(텍스처 왼쪽 아래 기준 x, y, w, h), 기준점(칸 기준 px), PPU, 색 수, 채도 무게.
# 석상은 바닥 타원 그림자, 박스 충돌이 받침대 크기에 맞춰져 있어 키를 처음 맵 그림과 같게 맞추고 폭은 새 그림 비율을 따른다.
# 처음 맵 그림의 PPU / 맵 배율이 모두 약 32라 같은 키, 같은 PPU로 두면 점 밀도도 32가 된다.
# v1(3/4 시점)은 64색 팔레트로 얼룩 범벅, 원본 해상도로 넣으니 쇠처럼 번들거리고 45도로 돌아 있어(사용자 지적) 정면, 무광, 큰 점으로 다시 뽑았다.
# v2 원본은 가짜 점이 4.3~5.3px로 정수가 아니라 격자째 자르면 석상마다 밀도가 달라진다. grid_pixelize 방식(색 먼저 나누고 칸 다수결)으로 맞춘다.
TARGETS = [
    ("발키리 석상.webp", "발키리 석상1.png", (34, 5, 92, 152), (46.0, 18.73), 32.0, 10, 1.0),
    ("기사 석상.webp", "기사 석상1.png", (36, 17, 46, 148), (23.0, 18.20), 28.75817, 10, 1.0),
    # 왕관 금색, 현수막 빨강, 상자, 꽃이 넓이가 작아 돌색에 묻히지 않게 색을 더 두고 채도에 무게를 둔다(분수대와 같음)
    ("왕 석상.webp", "왕 석상2.png", (50, 12, 159, 237), (79.5, 28.77), 51.8, 18, 3.0),
]
# 왕 석상은 다리 사이(약 3900px), 깃발 아래(약 2100~2400px)에 흰 배경이 갇혀 남았다. 흰 꽃잎(150px 미만)은 남도록 이 넓이 이상만 배경으로 본다.
OPEN_HOLES = {"왕 석상2.png": 1000}


def main():
    os.makedirs(BACKUP, exist_ok=True)
    result = {}
    for src_name, dst_name, rect, pivot, ppu, colors, chroma_w in TARGETS:
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
        out = pp.outline(gp.pixelize(rgba, w, h, colors, gp.ALPHA_MIN, chroma_w), "ink", deep=False)
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        # 기준점은 가로 가운데, 높이는 처음 그림 상자 안 비율 자리(받침대 바닥)를 그대로 옮긴다
        piv = [w / 2, max(0.0, float((pivot[1] - by0) / bh * h))]
        result[dst_name] = {"size": [w, h], "pivot": [round(piv[0], 2), round(piv[1], 2)], "ppu": ppu}
        print(dst_name, (w, h), "old box", (int(bw), int(bh)), result[dst_name])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
