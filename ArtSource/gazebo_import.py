# 새로 뽑은 왕실 정자 그림(투명 배경)을 지금 정자 크기, 64색, 검은 외곽선으로 바꿔 맵 그림에 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/gazebo_import.py
import os
import shutil
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import pixel_ppu32 as pp

SRC = os.path.join(bi.SRC, "왕실 정자.webp")
DST = os.path.join(bi.PROPS, "왕실 정자1.png")
# 다른 건물처럼 점을 2배로 키우면 지금 정자보다 37% 커져 길을 막는다. 그래서 지금 정자 그림 폭(137px)에 맞춰 PixelOE로 줄인다.
# 씬의 정자는 1.42배로 놓여 있고 그림 PPU가 45.3이라, 이 폭이면 점 밀도가 다른 건물(PPU 32, 1배)과 같다.
WIDTH = 137


def main():
    backup = os.path.join(bi.OLD_BACKUP, "왕실 정자1.png")
    if not os.path.exists(backup):
        shutil.copy2(DST, backup)
    a = np.asarray(Image.open(SRC).convert("RGBA")).astype(float) / 255.0
    ys, xs = np.nonzero(a[..., 3] > 0.5)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    a[..., 3] = a[..., 3] > 0.5
    oh = round(WIDTH * a.shape[0] / a.shape[1])
    out = pp.shrink(a, WIDTH, oh)
    out = pp.apply_palette(out, pp.load_palette())
    out = pp.outline(pp.remove_islands(out), "ink", deep=True)
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(DST)
    print("왕실 정자1.png", (WIDTH, oh))


if __name__ == "__main__":
    main()
