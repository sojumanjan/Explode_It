# 맵의 나무1 그림(생성 그림 색을 그대로 넣어 2500색)을 같은 크기에서 그림 전용 색 몇 개로 나눠 다른 소품처럼 면을 평평하게 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/tree_flatten.py
# 11/9 아트 점검: 다른 소품은 8~65색인데 나무만 2536색이라 픽셀 그림 사이에서 혼자 뿌옇게 보였다.
# 이미 맵 점 밀도(32)로 줄여 놓은 그림이라 크기는 그대로 두고 grid_pixelize의 색 나누기와 잔점 메우기만 쓴다. 외곽선 점도 색 하나로 묶인다.
import os
import shutil
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import grid_pixelize as gp
import pixel_ppu32 as pp

BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "tree_flatten_v0")
TARGET = "나무1.png"
COLORS = 16
CHROMA_W = 1.5  # 잎 연두와 줄기 갈색이 명암 단계에 묻히지 않게 채도에 조금 무게를 둔다


def main():
    os.makedirs(BACKUP, exist_ok=True)
    dst = os.path.join(bi.PROPS, TARGET)
    backup = os.path.join(BACKUP, TARGET)
    if not os.path.exists(backup):
        shutil.copy2(dst, backup)
    rgba = np.asarray(Image.open(backup).convert("RGBA")) / 255.0
    label, palette = gp.quantize(rgba, COLORS, CHROMA_W)
    label = gp.drop_specks(label)
    out = np.zeros_like(rgba)
    m = label >= 0
    out[m, :3] = palette[label[m]]
    out[m, 3] = 1
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
    used = np.unique((out[m][:, :3] * 255).round(), axis=0)
    print(TARGET, out.shape[1::-1], "colors", len(used))


if __name__ == "__main__":
    main()
