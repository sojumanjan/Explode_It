# 맵을 위에서 찍은 그림에서 울타리, 건물, 물을 피해 길을 잇고, 통짜 바닥 생성용 색 구역 콘티를 그리는 스크립트
# 사용: py -3.13 ArtSource/map_ground_conti.py
# 반복 타일은 넓은 맵에서 무늬가 되풀이되고 길, 광장 같은 구분이 없어 밋밋했다(11/18). 맵 배치에 맞춘 한 장짜리 바닥을 뽑기 위한 밑그림이다.
import heapq
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
LAYOUT = ROOT / "Docs/floor_mock/map_layout_raw.png"   # 2048px = 맵 바닥 64유닛, 1px = 게임 1px
OUT = ROOT / "Docs/floor_mock"
S = 2048
G = 256            # 길 찾기 칸 수(한 칸 8px = 0.25유닛)
CLEAR = 3          # 장애물에서 띄울 칸 수
PATH_W = 60        # 길 폭(px). 약 1.9유닛

# 구역 색. 생성기에 "이 색 = 이 바닥"으로 알려 준다.
DIRT = (196, 170, 128)
PATH = (226, 214, 184)
PLAZA = (176, 168, 156)
GRASS = (120, 158, 86)

# 1024 기준 좌표(맵 그림을 절반으로 본 좌표)
P = {
    "center": (510, 585), "gaz_w": (655, 560), "gaz_e": (880, 560), "gaz_n": (765, 425), "gaz_s": (765, 690),
    "fountain": (830, 790), "south": (800, 1015), "house_e": (950, 650), "guild": (800, 245),
    "tower": (565, 300), "house_w": (300, 300), "bridge": (115, 770), "king": (175, 860),
}
EDGES = [("center", "gaz_w"), ("gaz_s", "fountain"), ("fountain", "south"), ("gaz_e", "house_e"),
         ("gaz_n", "guild"), ("center", "bridge"), ("bridge", "king"), ("center", "tower"), ("tower", "house_w")]
PLAZAS = [("center", 105, 80), ("fountain", 120, 105), ("king", 75, 70)]
GAZEBO = ((765, 555), 98, 60)              # 정자 둘레 고리 길: 가운데, 바깥 반지름, 안쪽 반지름
GRASS_ZONES = [(880, 410, 1024, 720), (850, 860, 1024, 1024), (470, 950, 700, 1024), (0, 150, 175, 330), (640, 690, 730, 760)]


def obstacles(a: np.ndarray) -> np.ndarray:
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    green = (g > r + 8) & (g > b + 20)
    blue = b > r + 10
    bright = lum > 225
    dark = lum < 60
    red = r > g + 70
    return green | blue | bright | dark | red


def to_grid(mask: np.ndarray) -> np.ndarray:
    k = S // G
    m = mask.reshape(G, k, G, k).mean(axis=(1, 3)) > 0.25
    out = m.copy()
    for dy in range(-CLEAR, CLEAR + 1):
        for dx in range(-CLEAR, CLEAR + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def shortest(block: np.ndarray, a, b):
    # 장애물을 피하는 최단 길(8방향). 막혀 있으면 장애물 칸을 아주 비싸게 지나간다.
    sx, sy = a
    tx, ty = b
    dist = np.full(block.shape, np.inf)
    prev = {}
    dist[sy, sx] = 0
    q = [(0.0, sx, sy)]
    steps = [(1, 0, 1), (-1, 0, 1), (0, 1, 1), (0, -1, 1), (1, 1, 1.414), (1, -1, 1.414), (-1, 1, 1.414), (-1, -1, 1.414)]
    while q:
        d, x, y = heapq.heappop(q)
        if (x, y) == (tx, ty):
            break
        if d > dist[y, x]:
            continue
        for dx, dy, c in steps:
            nx, ny = x + dx, y + dy
            if 0 <= nx < G and 0 <= ny < G:
                nd = d + c * (60 if block[ny, nx] else 1)
                if nd < dist[ny, nx]:
                    dist[ny, nx] = nd
                    prev[(nx, ny)] = (x, y)
                    heapq.heappush(q, (nd, nx, ny))
    path, cur = [], (tx, ty)
    while cur != (sx, sy):
        path.append(cur)
        cur = prev[cur]
    path.append((sx, sy))
    return path[::-1]


def smooth(pts, n=4):
    pts = np.array(pts, float)
    for _ in range(n):
        pts[1:-1] = (pts[:-2] + pts[1:-1] * 2 + pts[2:]) / 4
    return pts


def main() -> None:
    a = np.asarray(Image.open(LAYOUT).convert("RGB")).astype(int)
    block = to_grid(obstacles(a))
    for name in P:
        x, y = P[name]
        block[y * G // 1024, x * G // 1024] = False
    zone = Image.new("RGB", (S, S), DIRT)
    d = ImageDraw.Draw(zone)
    for x0, y0, x1, y1 in GRASS_ZONES:
        d.rounded_rectangle((x0 * 2, y0 * 2, x1 * 2, y1 * 2), radius=60, fill=GRASS)
    cell = S / G
    for u, v in EDGES:
        g = [(int(P[k][0] * G / 1024), int(P[k][1] * G / 1024)) for k in (u, v)]
        pts = smooth(shortest(block, g[0], g[1])) * cell + cell / 2
        d.line([tuple(p) for p in pts], fill=PATH, width=PATH_W, joint="curve")
        for p in pts[[0, -1]]:
            d.ellipse((p[0] - PATH_W / 2, p[1] - PATH_W / 2, p[0] + PATH_W / 2, p[1] + PATH_W / 2), fill=PATH)
    (cx, cy), ro, ri = GAZEBO
    d.ellipse(((cx - ro) * 2, (cy - ro) * 2, (cx + ro) * 2, (cy + ro) * 2), fill=PATH)
    d.ellipse(((cx - ri) * 2, (cy - ri) * 2, (cx + ri) * 2, (cy + ri) * 2), fill=GRASS)
    for name, rx, ry in PLAZAS:
        x, y = P[name]
        d.ellipse(((x - rx) * 2, (y - ry) * 2, (x + rx) * 2, (y + ry) * 2), fill=PLAZA)
    # 가장자리를 조금 흐트러 손으로 칠한 경계처럼(생성기가 자로 그은 선을 따라 하지 않게)
    zone = zone.filter(ImageFilter.ModeFilter(9))
    zone.save(OUT / "map_conti_flat.png")
    over = Image.blend(Image.open(LAYOUT).convert("RGB"), zone, 0.55)
    over.save(OUT / "map_conti_overlay.png")
    over.resize((1024, 1024), Image.BOX).save(OUT / "map_conti_overlay_1024.png")


if __name__ == "__main__":
    main()
