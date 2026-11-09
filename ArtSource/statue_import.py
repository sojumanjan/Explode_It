# 새로 뽑은 석상 3종(흰 배경)을 원본 해상도 그대로 지금 맵 석상 자리에 같은 키, 같은 발밑 기준점으로 덮어쓴다
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
import pixel_ppu32 as pp
import prop_import as pi

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "statues_v1")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "statues_v0_map")
RESULT = os.path.join(pp.ROOT, "ArtSource", "statue_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 예전 스프라이트 칸(텍스처 왼쪽 아래 기준 x, y, w, h), 예전 기준점(칸 기준 px), 예전 PPU.
# 석상은 바닥 타원 그림자, 박스 충돌이 받침대 크기에 맞춰져 있어 넓이 대신 키를 예전과 같게 맞추고 폭은 새 그림 비율을 따른다.
TARGETS = [
    ("발키리 석상.webp", "발키리 석상1.png", (34, 5, 92, 152), (46.0, 18.73), 32.0),
    ("기사 석상.webp", "기사 석상1.png", (36, 17, 46, 148), (23.0, 18.20), 28.75817),
    ("왕 석상.webp", "왕 석상2.png", (50, 12, 159, 237), (79.5, 28.77), 51.8),
]


# 64색 팔레트로 다시 칠하고 외곽선, 점 정리를 하니 크림색 돌의 잔 명암이 끊겨 얼룩 범벅이 됐고, 줄이기만 해도 얼굴, 외곽선이 뭉개졌다.
# 석상은 한 색 계열이라 점 밀도가 주변보다 촘촘해도 덜 튄다(11/9 사용자 결정: 원본 그대로). 줄이지 않고 PPU를 올려 크기를 맞춘다.
def main():
    os.makedirs(BACKUP, exist_ok=True)
    result = {}
    for src_name, dst_name, rect, pivot, old_ppu in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        bx0, by0, bx1, by1 = pi.old_box(backup, rect)
        bw, bh = bx1 - bx0, by1 - by0
        rgba = pi.load_white_keyed(os.path.join(SRC, src_name), None, True)
        h, w = rgba.shape[:2]
        # 예전 그림의 월드 키(bh / 예전 PPU)와 같아지도록 PPU를 원본 키에 맞춰 올린다. 맵 배율은 그대로 둔다
        ppu = old_ppu * h / bh
        Image.fromarray((rgba * 255).round().astype(np.uint8), "RGBA").save(dst)
        # 기준점은 가로 가운데, 높이는 예전 그림 상자 안 비율 자리(받침대 바닥)를 그대로 옮긴다
        piv = [w / 2, max(0.0, (pivot[1] - by0) / bh * h)]
        result[dst_name] = {"size": [w, h], "pivot": [round(piv[0], 2), round(piv[1], 2)], "ppu": round(ppu, 4)}
        print(dst_name, (w, h), "old box", (bw, bh), result[dst_name])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
