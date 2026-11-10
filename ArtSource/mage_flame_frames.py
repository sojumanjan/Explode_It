# 화염 마법사 기본 그림의 손 위 불꽃만 흔들어 반복 그림 4장을 만든다(분수대 물결처럼 서 있어도 살아 있게)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/mage_flame_frames.py [미리보기 경로]
# 원본(fire_mage.png)은 사용자가 얼굴을 찍은 손질본이라 그대로 두고, 불꽃 칸만 바꾼 그림을 fire_mage_idle_0~3으로 쓴다(0은 원본과 같다).
# 소매 끝단도 불꽃과 같은 주황이라 색으로 고르지 않고 불꽃이 있는 칸(FLAME_BOX)만 건드린다.
# 불꽃과 외곽선을 한 덩어리로 줄마다 옮겨서, 아래(손에 닿는 곳)는 고정하고 위로 갈수록 좌우로 흔들린다.
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

SRC = os.path.join(pp.ART, "boss", "Fire_Mage", "fire_mage.png")
OUT = os.path.join(pp.ART, "boss", "Fire_Mage", "fire_mage_idle_{}.png")
FRAMES = 4
FLAME_BOX = (17, 15, 28, 30)   # (x0, y0, x1, y1) 끝 포함. 손바닥(31줄~) 위, 머리카락(29열~) 왼쪽
FIXED_ROWS = 3                 # 맨 아래 이 줄 수는 손에 붙어 있어 움직이지 않는다
SWAY = 2.0                     # 꼭대기 줄의 좌우 흔들림(점)
PHASE_PER_ROW = 0.45           # 위로 갈수록 늦게 따라와 불꽃이 일렁인다
CORE, INNER = (0xfd, 0xf0, 0x9d), (0xfc, 0xb5, 0x38)   # 가장 밝은 심지, 그 바깥 노랑


def main():
    src = np.asarray(Image.open(SRC).convert("RGBA")).copy()
    x0, y0, x1, y1 = FLAME_BOX
    box = np.zeros(src.shape[:2], bool)
    box[y0:y1 + 1, x0:x1 + 1] = True
    obj = box & (src[..., 3] > 127)
    height = y1 - FIXED_ROWS - y0 + 1
    frames = []
    for f in range(FRAMES):
        out = src.copy()
        out[obj] = 0
        for y in range(y0, y1 + 1):
            row = np.nonzero(obj[y])[0]
            if not len(row):
                continue
            up = max(0, (y1 - FIXED_ROWS) - y + 1)   # 고정 줄 위로 몇 번째 줄인지
            dx = int(round(SWAY * (up / height) * math.sin(2 * math.pi * f / FRAMES - up * PHASE_PER_ROW)))
            out[y, row + dx] = src[y, row]
        rgb = out[..., :3]
        m = box & (out[..., 3] > 127)
        core = m & np.all(rgb == CORE, -1)
        inner = m & np.all(rgb == INNER, -1)
        if f == 1:
            # 심지가 한 겹 넓어진다(심지 옆 노랑 일부가 밝아짐)
            near = np.zeros_like(core)
            near[1:] |= core[:-1]; near[:-1] |= core[1:]; near[:, 1:] |= core[:, :-1]; near[:, :-1] |= core[:, 1:]
            rgb[inner & near] = CORE
        elif f == 3:
            # 심지가 잠깐 어두워진다
            rgb[core] = INNER
        frames.append(out)
        Image.fromarray(out, "RGBA").save(OUT.format(f))
        print(OUT.format(f), "changed px", int((np.abs(out.astype(int) - src.astype(int)).sum(-1) > 0).sum()))
    if len(sys.argv) > 1:
        k = 10
        crop = [Image.fromarray(fr[0:40, 8:44], "RGBA") for fr in frames]
        sheet = Image.new("RGBA", (36 * k * FRAMES + 10 * (FRAMES + 1), 40 * k + 20), (126, 160, 80, 255))
        for i, c in enumerate(crop):
            sheet.alpha_composite(c.resize((36 * k, 40 * k), Image.NEAREST), (10 + i * (36 * k + 10), 10))
        sheet.save(sys.argv[1])


if __name__ == "__main__":
    main()
