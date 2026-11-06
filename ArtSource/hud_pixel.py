# 인게임 HUD 픽셀 조각(충전 점, 게이지 틀과 채움, 말풍선 상자)을 코드로 찍는다. 하트는 원래 그림(UI/heart.png)을 쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/hud_pixel.py
# HUD 그림 1px = 기준 해상도(1920x1080) 3칸(1080p 3px, 720p 2px). Unity에서 PPU 33.333, 점 필터로 둔다.
# 점, 게이지 채움은 코드가 색을 곱해 칠하므로 회색조로 밝고 어두운 면만 넣고, 외곽선은 캐릭터와 같은 잉크색이다.
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Project", "05. Art", "UI", "Pixel")
INK = (33, 23, 20, 255)
HI, BODY, SHADE = (255, 255, 255, 255), (214, 214, 214, 255), (160, 160, 160, 255)


def save(name, px):
    Image.fromarray(np.array(px, dtype=np.uint8), "RGBA").save(os.path.join(OUT, name))


def shaded(mask, light):
    # 겉 1px은 잉크, 안쪽은 light(x, y) 값(위 왼쪽이 밝다)으로 밝은 면, 몸통, 그늘을 나눈다
    h, w = mask.shape
    px = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            if not mask[y, x]:
                continue
            edge = any(not (0 <= y + dy < h and 0 <= x + dx < w and mask[y + dy, x + dx]) for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            v = light(x, y)
            px[y, x] = INK if edge else HI if v > 0.62 else BODY if v > 0.25 else SHADE
    return px


def pip():
    n = 9
    c = (n - 1) / 2
    m = np.array([[((x - c) ** 2 + (y - c) ** 2) <= 4.4 ** 2 for x in range(n)] for y in range(n)])
    light = lambda x, y: 1 - ((x - 2.5) ** 2 + (y - 2.5) ** 2) ** 0.5 / 5.5
    return shaded(m, light)


def gauge_frame():
    # 잉크 테두리 1px + 어두운 반투명 안쪽. 9-slice 테두리 2px
    n = 6
    px = np.zeros((n, n, 4), np.uint8)
    px[:, :] = (40, 34, 44, 170)
    px[0, :] = px[-1, :] = px[:, 0] = px[:, -1] = INK
    return px


def gauge_fill():
    # 가로로 늘려 쓰는 채움. 위 1줄 밝게, 아래 1줄 그늘
    px = np.zeros((7, 4, 4), np.uint8)
    px[:, :] = BODY
    px[0, :] = HI
    px[-1, :] = SHADE
    return px


def box():
    # 말풍선, 대사 상자, 알림 패널. 모서리 1px을 깎은 잉크 테두리, 흰 안쪽, 아래 안쪽 1줄 옅은 그늘. 9-slice 테두리 3px
    n = 8
    px = np.zeros((n, n, 4), np.uint8)
    px[1:-1, 1:-1] = HI
    px[-2, 1:-1] = (226, 222, 230, 255)
    px[0, 1:-1] = px[-1, 1:-1] = INK
    px[1:-1, 0] = px[1:-1, -1] = INK
    return px


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    save("hud_pip.png", pip())
    save("hud_gauge_frame.png", gauge_frame())
    save("hud_gauge_fill.png", gauge_fill())
    save("hud_box.png", box())
    print("ok", OUT)
