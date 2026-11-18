# 구역 그림의 길 배치 위에 평평한 톤 층(포스터화한 노이즈)을 깔아 맵 바닥(2048px)을 코드로 만드는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_ground_layers.py [출력 경로]
# 생성기(v7)는 톤 층을 2단계만, 길과 상관없는 구름 모양으로 칠해 어색했다(사용자). 층 수, 크기, 색 차이를 숫자로 직접 정한다.
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ZONES = ROOT / "Docs/floor_mock/map_conti_zones_v7.png"
DST = ROOT / "Assets/_Project/05. Art/UI/맵 바닥.png"
SIZE = 2048
SEED = 3
PATH = (152, 140, 120)   # 구역 그림 v7의 길 색

# 흙 톤 층. 어두운 쪽부터. 서로 가까운 회갈색이라 눈이 피곤하지 않게 대비를 낮게 둔다.
LAYERS = [(158, 147, 127), (165, 154, 133), (172, 160, 139), (179, 167, 146)]
LAYER_SHARE = [0.18, 0.32, 0.32, 0.18]   # 각 층이 차지하는 넓이 비율
# 길은 가장 어두운 흙 층보다도 어둡게 둔다. 흙 층보다 고정값만 어둡게 하면 밝은 층 위 길이 어두운 흙 층과 같은 색이 되어 묻혔다(v1).
# 길 안에서도 층 무늬는 작은 폭으로 이어져 띠처럼 납작해 보이지 않는다.
PATH_BASE = 10          # 가장 어두운 흙 층에서 길의 가장 밝은 톤까지 내려가는 밝기
PATH_STEP = 3           # 길 안 층 하나당 밝기 차이
OCTAVES = [(5, 1.0), (10, 0.5), (20, 0.22), (40, 0.08)]   # (격자 칸 수, 세기). 큰 덩어리 위주. 잔 무늬가 세면 얼룩무늬 군복처럼 보였다(v1)
WARP = 60               # 노이즈를 비트는 폭(px). 동글동글한 얼룩 대신 흐르는 등고선 모양
EDGE_NOISE = 5          # 길 가장자리 흔들림(px). 붓으로 그은 매끈한 띠처럼 보이지 않게
SMOOTH = 15             # 층 경계의 잔 점을 지우는 창 크기(홀수)
MIN_ISLAND = 6000       # 이보다 작은 층 섬(px, 약 2.5×2.5유닛)은 둘레 층으로 메운다


def fbm(rng: np.random.Generator) -> np.ndarray:
    n = np.zeros((SIZE, SIZE), np.float32)
    for cells, w in OCTAVES:
        g = rng.random((cells + 3, cells + 3)).astype(np.float32)
        big = cv2.resize(g, (SIZE + SIZE * 3 // cells, SIZE + SIZE * 3 // cells), interpolation=cv2.INTER_CUBIC)
        n += w * big[:SIZE, :SIZE]
    return (n - n.min()) / (n.max() - n.min())


def warp(n: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    dx = (fbm(rng) - 0.5) * 2 * WARP
    dy = (fbm(rng) - 0.5) * 2 * WARP
    return cv2.remap(n, xx + dx, yy + dy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)


def drop_islands(level: np.ndarray) -> np.ndarray:
    # 작은 섬은 바로 바깥 둘레에서 가장 많은 층으로 메운다. 메우면 이웃 섬이 커질 수 있어 몇 번 되풀이한다.
    ring_k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))
    for _ in range(3):
        changed = False
        for v in range(len(LAYERS)):
            n, lab, stats, _ = cv2.connectedComponentsWithStats((level == v).astype(np.uint8), 8)
            for i in range(1, n):
                if stats[i, cv2.CC_STAT_AREA] >= MIN_ISLAND:
                    continue
                x, y, w, h = stats[i, :4]
                x0, y0, x1, y1 = max(x - 3, 0), max(y - 3, 0), min(x + w + 3, SIZE), min(y + h + 3, SIZE)
                m = (lab[y0:y1, x0:x1] == i).astype(np.uint8)
                ring = (cv2.dilate(m, ring_k) > 0) & (m == 0)
                around = level[y0:y1, x0:x1][ring]
                if len(around):
                    level[y0:y1, x0:x1][m > 0] = np.bincount(around, minlength=len(LAYERS)).argmax()
                    changed = True
        if not changed:
            break
    return level


def main() -> None:
    dst = Path(sys.argv[1]) if len(sys.argv) > 1 else DST
    rng = np.random.default_rng(SEED)
    n = warp(fbm(rng), rng)
    cuts = np.quantile(n, np.cumsum(LAYER_SHARE)[:-1])
    level = np.digitize(n, cuts).astype(np.uint8)
    level = drop_islands(cv2.medianBlur(level, SMOOTH))

    z = np.asarray(Image.open(ZONES).convert("RGB")).astype(np.int32)
    path = (np.abs(z - np.array(PATH)).sum(-1) < 6).astype(np.uint8)
    # 길 경계를 부호 거리로 두고 잔 노이즈를 더해 흔든다
    inside = cv2.distanceTransform(path, cv2.DIST_L2, 5)
    outside = cv2.distanceTransform(1 - path, cv2.DIST_L2, 5)
    sd = outside - inside
    edge = (fbm(np.random.default_rng(SEED + 1)) - 0.5) * 2 * EDGE_NOISE
    path = cv2.medianBlur(((sd + edge) < 0).astype(np.uint8), 5).astype(bool)

    out = np.array(LAYERS, np.int32)[level]
    darkest = np.array(LAYERS[0], np.int32)
    top = len(LAYERS) - 1
    out[path] = darkest - PATH_BASE - (top - level[path])[:, None] * PATH_STEP
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(dst)
    print(dst.name, "colors", len(np.unique(out.reshape(-1, 3), axis=0)))


if __name__ == "__main__":
    main()
