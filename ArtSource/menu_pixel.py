# 메인 메뉴(책) 페이지를 픽셀로 만든다: 책 배경, 장비 칸, 잠김 표시(11/8 사용자, 메인 페이지만 먼저)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/menu_pixel.py
# 점 크기는 타이틀 버튼, Galmuri 글자와 같은 4배(그림 1px = 기준 해상도 4칸). 책은 화면 1420x968 -> 355x242.
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

BOOK_SRC = os.path.join(pp.ART, "UI", "MainMenuBook.png")
BOOK_RECT = (12, 500 - 103 - 324, 12 + 476, 500 - 103)  # 스프라이트 영역(유니티 좌표는 아래에서 위)
BOOK_DST = os.path.join(pp.ART, "UI", "Pixel", "menu_book.png")
SLOT_DST = os.path.join(pp.ART, "UI", "Pixel", "menu_slot.png")
BADGE_DST = os.path.join(pp.ART, "UI", "Pixel", "menu_badge.png")
BUTTON_DST = os.path.join(pp.ART, "UI", "Pixel", "menu_button.png")
BOOK_SIZE = (355, 242)
BOOK_COLORS = 12
BOOK_CLEAN_PASSES = 2
# 검은 잉크 테두리는 책 위에서 너무 튀어(사용자 지적) 갈색으로 둔다.
# 칸은 흰색을 곱하므로 이 색 그대로, 버튼은 주황을 곱하므로 회색으로 두면 버튼 색의 진한 갈색이 된다.
SLOT_EDGE = (120, 82, 52)
BUTTON_EDGE = (128, 128, 128)


def book():
    rgba = np.asarray(Image.open(BOOK_SRC).convert("RGBA").crop(BOOK_RECT)) / 255.0
    out = pp.shrink(rgba, *BOOK_SIZE)
    m = out[..., 3] > 0.5
    lab = pp.oklab(out[m][:, :3])
    centers = pp.kmeans(lab, BOOK_COLORS)
    idx = np.argmin(((lab[:, None] - centers[None]) ** 2).sum(-1), 1)
    out[m, :3] = np.clip(pp.from_oklab(centers[idx]), 0, 1)
    out[..., 3] = m.astype(float)
    # 원본 종이 결 때문에 테두리와 책등이 1px씩 들쭉날쭉하고 종이에 점이 박혀 보였다(사용자 지적).
    # 색 번호 판에서 다수결로 정리한다: 가로·세로 이웃 각각 5칸에서 더 많이 나온 색으로 맞추면 곧은 선은 남고 튀는 점은 사라진다.
    lab_all = pp.oklab(out[..., :3].reshape(-1, 3))
    label = np.argmin(((lab_all[:, None] - centers[None]) ** 2).sum(-1), 1).reshape(m.shape)
    label[~m] = -1
    for _ in range(BOOK_CLEAN_PASSES):
        label = clean(label)
    out[..., :3] = np.where(label[..., None] >= 0, np.clip(pp.from_oklab(centers[np.maximum(label, 0)]), 0, 1), 0)
    out[..., 3] = (label >= 0).astype(float)
    pp.save(BOOK_DST, out)


def clean(label):
    h, w = label.shape
    pad = np.pad(label, 2, mode="edge")
    out = label.copy()
    for y in range(h):
        for x in range(w):
            if label[y, x] < 0:
                continue
            col = pad[y:y + 5, x + 2]
            row = pad[y + 2, x:x + 5]
            best, best_n = label[y, x], 0
            for line in (col, row):
                vals, counts = np.unique(line[line >= 0], return_counts=True)
                i = counts.argmax()
                if counts[i] > best_n:
                    best, best_n = vals[i], counts[i]
            # 가로나 세로 한 줄에서 3칸 이상 같은 색이면 그 색이 이 칸의 선이다
            if best_n >= 3:
                out[y, x] = best
    return out


def slot():
    # 장비 칸: 모서리 1px을 깎은 갈색 테두리 안을 흰색으로 채운다(색은 씬 값을 곱한다). 9-slice 테두리 3px
    n = 9
    px = np.zeros((n, n, 4), np.uint8)
    px[1:-1, 1:-1] = (255, 255, 255, 255)
    px[0, 1:-1] = px[-1, 1:-1] = SLOT_EDGE + (255,)
    px[1:-1, 0] = px[1:-1, -1] = SLOT_EDGE + (255,)
    Image.fromarray(px, "RGBA").save(SLOT_DST)


def button():
    # 타이틀 버튼과 같은 모양(위 안쪽 1줄 밝게, 아래 안쪽 2줄 그늘)에 테두리만 회색. 9-slice 테두리 3px
    n = 9
    px = np.zeros((n, n, 4), np.uint8)
    px[1:-1, 1:-1] = (232, 232, 232, 255)
    px[1, 1:-1] = (255, 255, 255, 255)
    px[-3:-1, 1:-1] = (186, 186, 186, 255)
    px[0, 1:-1] = px[-1, 1:-1] = BUTTON_EDGE + (255,)
    px[1:-1, 0] = px[1:-1, -1] = BUTTON_EDGE + (255,)
    Image.fromarray(px, "RGBA").save(BUTTON_DST)


def badge():
    # 잠김 딱지: 모서리 1px만 깎은 한 색 판(색은 씬 값). 9-slice 테두리 2px
    n = 6
    px = np.full((n, n, 4), 255, np.uint8)
    for y, x in ((0, 0), (0, n - 1), (n - 1, 0), (n - 1, n - 1)):
        px[y, x] = (0, 0, 0, 0)
    Image.fromarray(px, "RGBA").save(BADGE_DST)


if __name__ == "__main__":
    book()
    slot()
    button()
    badge()
    print("ok")
