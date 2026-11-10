# 최종 보스 3국면 "별의 의식" 바닥 마법진을 새 그림(초록 배경)에서 맵 밀도(1유닛 32점)로 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/star_sigil_import.py [미리보기 경로]
# 코드는 그림 폭의 44% 지점을 별 꼭짓점으로 보고 별 반지름(7유닛)에 맞춰 늘린다(AwakenedKingBoss.StarSpriteTipRatio).
# 왕은 정오각별 꼭짓점으로 순간이동하므로, 그려진 꼭짓점이 그 자리와 어긋나면 엉뚱한 곳에 서 보인다.
# 생성 그림은 아래 두 꼭짓점이 약 35px 바깥, 좌우 꼭짓점이 16px 아래라, 가운데를 고정하고 꼭짓점만 정자리로 옮기는 각도·반지름 변형을 한다.
import math
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import grid_pixelize as gp
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "boss_restyle", "star_sigil_v1.webp")
DST = os.path.join(pp.ART, "King", "superking_crown_star.png")
OUT_PX = 512        # 그림 한 변. 별 반지름 7유닛 / 0.44 = 15.9유닛 x 32 = 509점에 가깝게
TIP_RATIO = 0.44    # AwakenedKingBoss.StarSpriteTipRatio와 같아야 한다
COLORS = 24
TIP_SEARCH = 70     # 정자리에서 이 반경 안의 가장 먼 점을 실제 꼭짓점으로 본다


def foreground(a):
    # 마법진에는 초록이 없어서, 고리와 별 사이에 갇힌 곳까지 초록은 모두 배경으로 본다
    green = (a[..., 1] > 150) & (a[..., 1] - np.maximum(a[..., 0], a[..., 2]) > 60)
    return ~green


GLOW_DST = os.path.join(pp.ART, "King", "superking_star_glow.png")
GLOW_COLOR = (150, 60, 230)
# 마법진 뒤에 까는 보라 빛. 픽셀 그림과 맞게 부드러운 그라데이션 대신 계단식 동심원으로 진하기를 줄인다.
# (바깥 반지름(그림 한 변 대비), 불투명도). 별 꼭짓점이 0.44라 그보다 조금 넘치게 감싼다
GLOW_BANDS = [(0.35, 0.55), (0.40, 0.45), (0.44, 0.34), (0.475, 0.22), (0.50, 0.11)]


def make_glow():
    n = OUT_PX
    yy, xx = np.mgrid[0:n, 0:n] + 0.5
    r = np.hypot(xx - n / 2, yy - n / 2) / n
    alpha = np.zeros((n, n))
    for edge, a in reversed(GLOW_BANDS):
        alpha[r <= edge] = a
    out = np.zeros((n, n, 4), np.uint8)
    out[..., :3] = GLOW_COLOR
    out[..., 3] = (alpha * 255).round().astype(np.uint8)
    Image.fromarray(out, "RGBA").save(GLOW_DST)
    print("saved", GLOW_DST)


def main():
    make_glow()
    a = np.asarray(Image.open(SRC).convert("RGB"))
    fg = foreground(a.astype(int))
    ys, xs = np.nonzero(fg)
    # 위 꼭짓점과 좌우 꼭짓점 폭으로 정오각별 중심과 반지름을 잡는다(아래쪽은 고리가 꼭짓점보다 더 내려와 못 쓴다)
    top, left, right = ys.min(), xs.min(), xs.max()
    R = (right - left) / (2 * math.sin(math.radians(72)))
    cx, cy = (left + right) / 2, top + R
    want_ang, have_ang, have_r = [], [], []
    for k in range(5):
        t = math.radians(90 - 72 * k)
        ex, ey = cx + R * math.cos(t), cy - R * math.sin(t)
        near = (xs - ex) ** 2 + (ys - ey) ** 2 < TIP_SEARCH ** 2
        d = np.hypot(xs[near] - cx, ys[near] - cy)
        i = d.argmax()
        tx, ty = xs[near][i], ys[near][i]
        want_ang.append(math.degrees(t))
        have_ang.append(math.degrees(math.atan2(cy - ty, tx - cx)))
        have_r.append(d[i])
        print(f"tip {k}: want ({ex:.0f},{ey:.0f}) have ({tx},{ty}) r {d[i]:.0f}/{R:.0f}")
    # 출력 좌표의 각도(정자리 기준)를 원본 각도로, 반지름을 꼭짓점 비율만큼 늘려 원본에서 가져온다. 꼭짓점 사이는 선형으로 잇는다
    want = np.unwrap(np.radians(want_ang + [want_ang[0] - 360]))
    have = np.unwrap(np.radians(have_ang + [have_ang[0] - 360]))
    ratio = np.array(have_r + [have_r[0]]) / R
    order = np.argsort(-want)
    want, have, ratio = want[order], have[order], ratio[order]
    h, w = fg.shape
    S = R / TIP_RATIO   # 출력 한 변(원본 px)
    k = S / OUT_PX
    n = int(round(S))
    gy, gx = np.mgrid[0:n, 0:n].astype(np.float64)
    ox, oy = gx - n / 2 + 0.5, -(gy - n / 2 + 0.5)
    ang = np.arctan2(oy, ox)
    ang = np.where(ang > want[0], ang - 2 * np.pi, ang)   # want는 90도에서 -270도로 줄어드는 순서
    src_ang = np.interp(-ang, -want, -have) * -1
    src_r = np.hypot(ox, oy) * np.interp(-ang, -want, ratio)
    mx = (cx + src_r * np.cos(src_ang)).astype(np.float32)
    my = (cy - src_r * np.sin(src_ang)).astype(np.float32)
    rgba = np.dstack([a, fg.astype(np.uint8) * 255])
    warped = cv2.remap(rgba, mx, my, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=0)
    src = warped / 255.0
    src[..., 3] = (src[..., 3] > 0.5).astype(float)
    label, pal = gp.quantize(src, COLORS, 1.5)
    # 외곽선과 초록 배경이 섞인 가장자리 색은 외곽선(가장 어두운 색)으로 바꾼다. 마법진에는 초록이 없다
    greenish = (pal[:, 1] > pal[:, 0] + 0.03) & (pal[:, 1] > pal[:, 2] + 0.03)
    if greenish.any():
        darkest = np.argmin(pp.lum(np.where(greenish[:, None], 9, pal)))
        label = np.where(np.isin(label, np.nonzero(greenish)[0]), darkest, label)
        print("greenish colors -> outline:", int(greenish.sum()))
    small = gp.drop_specks(gp.mode_downsample(label, OUT_PX, OUT_PX, gp.ALPHA_MIN))
    out = np.zeros((OUT_PX, OUT_PX, 4))
    m = small >= 0
    out[m, :3] = pal[small[m]]
    out[m, 3] = 1
    out = pp.remove_islands(out)
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(DST)
    print("saved", DST, "colors", len(np.unique(small[m])), "source px per dot", round(k, 3))
    if len(sys.argv) > 1:
        # 미리보기: 결과 위에 코드가 쓰는 꼭짓점 자리를 파란 점으로
        prev = Image.fromarray((out * 255).round().astype(np.uint8), "RGBA")
        bg = Image.new("RGBA", prev.size, (126, 160, 80, 255))
        bg.alpha_composite(prev)
        px = bg.load()
        c = OUT_PX / 2
        for kk in range(5):
            t = math.radians(90 - 72 * kk)
            x, y = c + TIP_RATIO * OUT_PX * math.cos(t), c - TIP_RATIO * OUT_PX * math.sin(t)
            for dx in range(-2, 3):
                for dy in range(-2, 3):
                    px[int(x) + dx, int(y) + dy] = (0, 80, 255, 255)
        bg.resize((OUT_PX * 2, OUT_PX * 2), Image.NEAREST).save(sys.argv[1])


if __name__ == "__main__":
    main()
