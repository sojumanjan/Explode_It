# 설정 페이지용 픽셀 부품(슬라이더 손잡이, 체크 표시, 펼침 화살표)을 만든다. 점 크기는 메뉴 칸과 같은 4배(PPU 25)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/menu_pixel_parts.py
import os

import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "05. Art", "UI", "Pixel")
EDGE = (120, 82, 52, 255)
CLEAR = (0, 0, 0, 0)
WHITE = (255, 255, 255, 255)


def knob():
    # 메뉴 버튼과 같은 결(위 밝게, 아래 그늘)에 갈색 테두리, 모서리 1px 깎기. 색은 씬 값을 곱한다
    w, h = 7, 9
    px = np.zeros((h, w, 4), np.uint8)
    px[1:-1, 1:-1] = (232, 232, 232, 255)
    px[1, 1:-1] = WHITE
    px[-3:-1, 1:-1] = (186, 186, 186, 255)
    px[0, 1:-1] = px[-1, 1:-1] = EDGE
    px[1:-1, 0] = px[1:-1, -1] = EDGE
    Image.fromarray(px, "RGBA").save(os.path.join(OUT, "menu_knob.png"))


def check():
    # 굵기 2px 체크. 흰색으로 두고 색은 씬 값
    rows = [
        "..........",
        "........##",
        ".......###",
        "......###.",
        "##...###..",
        "###.###...",
        ".#####....",
        "..###.....",
        "...#......",
        "..........",
    ]
    save_mask(rows, "menu_check.png")


def arrow():
    # 아래를 향한 삼각형
    rows = [
        "#######",
        ".#####.",
        "..###..",
        "...#...",
    ]
    save_mask(rows, "menu_arrow.png")


def save_mask(rows, name):
    h, w = len(rows), len(rows[0])
    px = np.zeros((h, w, 4), np.uint8)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            px[y, x] = WHITE if ch == "#" else CLEAR
    Image.fromarray(px, "RGBA").save(os.path.join(OUT, name))


if __name__ == "__main__":
    knob()
    check()
    arrow()
    print("ok")
