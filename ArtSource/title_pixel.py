# 타이틀 화면 로고와 버튼을 픽셀로 만든다(메뉴 책 쪽은 매끈하게 둔다, 11/6 사용자)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/title_pixel.py
# 로고는 타이틀 배경, 고블린과 같은 8배(그림 1px = 기준 해상도 8칸): 960x400 -> 120x50.
# 버튼은 글자와 같은 4배: 9-slice 조각을 그려 두고 코드가 색을 곱해 칠한다.
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

ROOT = pp.ROOT
LOGO_SRC = os.path.join(ROOT, "ArtSource", "Original", "UI", "TitleLogo_current.png")
LOGO_DST = os.path.join(pp.ART, "UI", "TitleLogo_pixel.png")
BUTTON_DST = os.path.join(pp.ART, "UI", "Pixel", "title_button.png")
SHADOW_DST = os.path.join(pp.ART, "UI", "Pixel", "title_button_shadow.png")
LOGO_SCALE = 8
LOGO_COLORS = 20     # 타이틀 배경(20색)과 같게
INK = (33, 23, 20)


def logo():
    rgba = np.asarray(Image.open(LOGO_SRC).convert("RGBA")) / 255.0
    h, w = rgba.shape[:2]
    out = pp.remove_islands(pp.shrink(rgba, w // LOGO_SCALE, h // LOGO_SCALE))
    # 로고 안에서만 k-means로 색을 줄여 픽셀 덩어리를 만든다
    m = out[..., 3] > 0.5
    lab = pp.oklab(out[m][:, :3])
    centers = pp.kmeans(lab, LOGO_COLORS)
    idx = np.argmin(((lab[:, None] - centers[None]) ** 2).sum(-1), 1)
    out[m, :3] = np.clip(pp.from_oklab(centers[idx]), 0, 1)
    out[..., 3] = m.astype(float)
    # 외톨이 픽셀(상하좌우가 모두 다른 색)은 가장 많은 이웃 색으로 흡수해 글자 면의 자글거림을 없앤다
    for _ in range(2):
        src = out.copy()
        for y in range(1, out.shape[0] - 1):
            for x in range(1, out.shape[1] - 1):
                if not m[y, x]:
                    continue
                nb = [tuple(src[y + dy, x + dx, :3]) for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)) if m[y + dy, x + dx]]
                # 어두운 칸은 글자 사이 1px 선이라 남긴다(지우면 x, p, l 구멍이 막힘)
                if pp.oklab(src[y, x, :3][None])[0, 0] < 0.55:
                    continue
                if len(nb) == 4 and all(c != tuple(src[y, x, :3]) for c in nb):
                    out[y, x, :3] = max(set(nb), key=nb.count)
    pp.save(LOGO_DST, out)


def button():
    # 모서리 1px을 깎은 잉크 테두리, 위 안쪽 1줄 밝게, 아래 안쪽 2줄 그늘. 9-slice 테두리 3px
    n = 9
    px = np.zeros((n, n, 4), np.uint8)
    px[1:-1, 1:-1] = (232, 232, 232, 255)
    px[1, 1:-1] = (255, 255, 255, 255)
    px[-3:-1, 1:-1] = (186, 186, 186, 255)
    px[0, 1:-1] = px[-1, 1:-1] = INK + (255,)
    px[1:-1, 0] = px[1:-1, -1] = INK + (255,)
    Image.fromarray(px, "RGBA").save(BUTTON_DST)
    # 그림자: 같은 모양을 한 색으로 채운다(색과 투명도는 씬 값)
    sh = np.zeros((n, n, 4), np.uint8)
    sh[1:-1, :] = (255, 255, 255, 255)
    sh[0, 1:-1] = sh[-1, 1:-1] = (255, 255, 255, 255)
    Image.fromarray(sh, "RGBA").save(SHADOW_DST)


if __name__ == "__main__":
    logo()
    button()
    print("ok")
