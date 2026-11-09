# 나무 흔들림 시트(6장, 조금씩 다른 나무)를 나무1과 같은 크기, 같은 밑동 자리의 반복 그림으로 바꾼다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/tree_sway_import.py [--all]
# 장마다 따로 색을 나누면 같은 잎이 장마다 다른 색이 돼 번쩍인다. 그래서 고른 장을 한꺼번에 색을 나누고(같은 팔레트), 칸 다수결만 장마다 한다.
# 장마다 그림 키가 조금 달라(위 줄 3장이 아래 줄보다 약 4% 큼) 같은 배율로 줄이면 나무가 들썩인다. 장마다 키를 나무1 키에 맞춘다.
# 밑동 자리는 뿌리 바로 위 줄기 가운데로 맞춰 줄기는 서 있고 잎만 움직이게 한다.
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import grid_pixelize as gp
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "tree_sway", "나무 흔들림.webp")
OUT = os.path.join(bi.PROPS, "Life")
BG_MAX = 30       # 검은 배경으로 볼 밝기(채널 최댓값). 외곽선은 다시 그리므로 같이 지워져도 된다
OW, OH = 129, 139  # 나무1 키(139)에 좌우 여유 2점씩. PPU는 나무1과 같게 둬 놓인 크기가 같다
ANCHOR_X = 64.5   # 밑동이 올 출력 x(가운데)
COLORS = 16
CHROMA_W = 1.5    # 나무1과 같음
# 시트 순서(위 줄 왼쪽부터 0~5) 중 쓸 장. 6장의 기울기 차이는 1점 안쪽이고 잎 무늬가 다르다. 이웃한 장끼리 다른 점이 가장 적게 이어지는 순서로 둬 일렁임이 튀지 않게 한다.
PICK = [3, 5, 4]  # 위 줄 1번째(0)는 사용자가 뺌(11/9)
PREVIEW = os.environ.get("TREE_PREVIEW", ".")  # --all 미리보기를 쓸 폴더(프로젝트 밖)


def split(a):
    h, w = a.shape[:2]
    fg = a.max(-1) > BG_MAX
    cells = []
    for r in range(2):
        y0, y1 = r * h // 2, (r + 1) * h // 2
        col = fg[y0:y1].any(0)
        x = 0
        while x < w:
            if col[x]:
                s = x
                while x < w and col[x]:
                    x += 1
                if x - s > 50:
                    band = fg[y0:y1, s:x]
                    ys = np.nonzero(band.any(1))[0]
                    cells.append((s, y0 + ys[0], x, y0 + ys[-1] + 1))
            x += 1
    return cells


def frame_canvas(a, box):
    x0, y0, x1, y1 = box
    crop = a[y0:y1, x0:x1]
    opaque = crop.max(-1) > BG_MAX
    hh = crop.shape[0]
    # 밑동: 맨 아래에서 35~55점 위(뿌리 위, 잎 아래) 줄기 점들의 가운데
    trunk = opaque[hh - 55:hh - 35]
    tx = np.nonzero(trunk)[1]
    base_x = tx.mean()
    k = hh / OH  # 원본 점 / 출력 점
    sw = int(round(OW * k))
    left = int(round(base_x - ANCHOR_X * k))
    canvas = np.zeros((hh, sw, 4))
    for cx in range(sw):
        sx = left + cx
        if 0 <= sx < crop.shape[1]:
            canvas[:, cx, :3] = crop[:, sx] / 255.0
            canvas[:, cx, 3] = opaque[:, sx]
    return canvas


def main():
    a = np.asarray(Image.open(SRC).convert("RGB")).astype(float)
    cells = split(a)
    pick = range(len(cells)) if "--all" in sys.argv else PICK
    canvases = [frame_canvas(a, cells[i]) for i in pick]
    # 한꺼번에 색 나누기: 높이를 맞춰 가로로 붙인다(빈 칸은 투명이라 색 나누기에 안 들어간다)
    hmax = max(c.shape[0] for c in canvases)
    joined = np.concatenate([np.pad(c, ((0, hmax - c.shape[0]), (0, 0), (0, 0))) for c in canvases], 1)
    label, palette = gp.quantize(joined, COLORS, CHROMA_W)
    os.makedirs(OUT, exist_ok=True)
    x = 0
    for n, (i, c) in enumerate(zip(pick, canvases)):
        lab = label[:c.shape[0], x:x + c.shape[1]]
        x += c.shape[1]
        small = gp.drop_specks(gp.mode_downsample(lab, OW, OH, gp.ALPHA_MIN))
        out = np.zeros((OH, OW, 4))
        m = small >= 0
        out[m, :3] = palette[small[m]]
        out[m, 3] = 1
        out = pp.outline(pp.remove_islands(out), "ink", deep=False)
        name = f"나무1_흔들_{n}.png" if "--all" not in sys.argv else f"_tree_sheet_{i}.png"
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(os.path.join(PREVIEW if "--all" in sys.argv else OUT, name))
        print(name, "sheet", i, out.shape[1::-1])


if __name__ == "__main__":
    main()
