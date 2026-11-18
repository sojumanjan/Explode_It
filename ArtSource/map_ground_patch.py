# 생성한 맵 바닥 원본에서 지정한 구역(풀뭉텅이, 돌 광장)을 지우고 둘레 흙으로 자연스럽게 메우는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_ground_patch.py
# 결과(map_ground_v2_edit.png)를 map_ground_pixelize.py에 넣는다.
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
DIR = ROOT / "ArtSource/Original/ground"
SRC = DIR / "map_ground_v2.webp"
DST = DIR / "map_ground_v2_edit.png"

# 지울 구역(원본 2000px 기준). 사용자가 v2에서 왼쪽 풀뭉텅이와 분수대 쪽 돌 광장만 빼 달라고 했다.
GRASS_BOX = (260, 800, 780, 1360)       # 왼쪽 풀뭉텅이를 감싸는 상자(x0, y0, x1, y1)
STONE_CENTER, STONE_RADIUS = (1658, 1662), 236   # 돌 광장 원(테두리 돌 포함)
OUTLINE = 16          # 풀 외곽선, 그림자까지 덮게 넓히는 폭(px)
CLEAN_BOX = (800, 60, 1400, 360)   # 길, 풀 없이 흙만 있는 자리. 메운 곳에 입힐 흙 결을 여기서 가져온다
ZONES = ROOT / "Docs/floor_mock/map_conti_zones_v2.png"   # v2를 뽑은 구역 그림. 길 자리를 여기서 안다
ZONE_PATH = (168, 144, 108)
PATH_MARGIN = 30      # 생성 그림의 길이 구역 그림보다 조금 넓거나 비켜 그려져서 넓혀 뺀다(px)
BASE_SIGMA = 40       # 흙 바탕색을 둘레에서 모을 때 흐림 폭. 넓은 밝기 흐름(가장자리 어둑함)이 이어진다
# 처음에는 둘레 그림을 그대로 번지게 메웠더니 길, 풀 외곽선의 회색이 번져 얼룩이 되었다. 길과 지울 구역 둘레는 빼고 흙만 모은다.
FEATHER = 31          # 메운 곳과 원래 그림의 경계를 섞는 폭(홀수). 넓게 두어 끊긴 길 끝이 원본 길 끝처럼 흐리게 사라진다


def grass_mask(a: np.ndarray) -> np.ndarray:
    hsv = cv2.cvtColor(a, cv2.COLOR_RGB2HSV)
    m = ((hsv[..., 0] > 25) & (hsv[..., 0] < 75) & (hsv[..., 1] > 50)).astype(np.uint8)
    box = np.zeros_like(m)
    x0, y0, x1, y1 = GRASS_BOX
    box[y0:y1, x0:x1] = 1
    m &= box
    # 풀 사이 구멍을 메우고 외곽선, 그림자까지 넓힌다
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (25, 25)))
    n, lab, stats, _ = cv2.connectedComponentsWithStats(m, 8)
    keep = np.zeros(n, bool)
    keep[1:] = stats[1:, cv2.CC_STAT_AREA] >= 5000
    m = keep[lab].astype(np.uint8)
    return cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (OUTLINE * 2 + 1, OUTLINE * 2 + 1)))


def stone_mask(shape) -> np.ndarray:
    m = np.zeros(shape[:2], np.uint8)
    cv2.circle(m, STONE_CENTER, STONE_RADIUS, 1, -1)
    return m


def texture(shape) -> np.ndarray:
    # 깨끗한 흙의 잔 결(원본 - 흐린 원본)만 떼어 거울 반복으로 그림 크기만큼 깐다
    x0, y0, x1, y1 = CLEAN_BOX
    a = np.asarray(Image.open(SRC).convert("RGB")).astype(np.float32)[y0:y1, x0:x1]
    detail = a - cv2.GaussianBlur(a, (0, 0), 12)
    h, w = shape[:2]
    rows = []
    for ty in range(0, h, detail.shape[0]):
        row = []
        for tx in range(0, w, detail.shape[1]):
            t = detail
            if (tx // detail.shape[1]) % 2:
                t = t[:, ::-1]
            if (ty // detail.shape[0]) % 2:
                t = t[::-1]
            row.append(t)
        rows.append(np.concatenate(row, 1))
    return np.concatenate(rows, 0)[:h, :w]


def main() -> None:
    a = np.asarray(Image.open(SRC).convert("RGB"))
    mask = grass_mask(a) | stone_mask(a.shape)
    h, w = mask.shape
    z = np.asarray(Image.open(ZONES).convert("RGB").resize((w, h), Image.NEAREST)).astype(np.int32)
    path = (np.abs(z - np.array(ZONE_PATH)).sum(-1) < 6).astype(np.uint8)
    k = lambda r: cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (r * 2 + 1, r * 2 + 1))
    exclude = cv2.dilate(path, k(PATH_MARGIN)) | cv2.dilate(mask, k(20))
    # 흙 자리만 가중치로 모아 흐린다(빠진 자리는 둘레 흙 색으로 채워진다)
    wgt = (1 - exclude).astype(np.float32)
    num = cv2.GaussianBlur(a.astype(np.float32) * wgt[..., None], (0, 0), BASE_SIGMA)
    den = cv2.GaussianBlur(wgt, (0, 0), BASE_SIGMA)[..., None]
    base = num / np.maximum(den, 1e-3)
    # 지울 구역 한가운데는 흐림 폭이 닿지 않아 흙이 모이지 않는다(검게 비었다). 줄인 그림에서 넓게 모은 색으로 채운다.
    sw, sh = w // 8, h // 8
    s_wgt = cv2.resize(wgt, (sw, sh), interpolation=cv2.INTER_AREA)
    s_num = cv2.resize(a.astype(np.float32) * wgt[..., None], (sw, sh), interpolation=cv2.INTER_AREA)
    wide = cv2.GaussianBlur(s_num, (0, 0), BASE_SIGMA / 2) / np.maximum(cv2.GaussianBlur(s_wgt, (0, 0), BASE_SIGMA / 2), 1e-3)[..., None]
    wide = cv2.resize(wide, (w, h), interpolation=cv2.INTER_CUBIC)
    t = np.clip(den / 0.3, 0, 1)
    base = t * base + (1 - t) * wide
    fill = base + texture(a.shape)
    alpha = cv2.GaussianBlur(cv2.dilate(mask, k(6)).astype(np.float32), (FEATHER, FEATHER), 0)[..., None]
    out = a.astype(np.float32) * (1 - alpha) + fill * alpha
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(DST)
    print(DST.name, "masked px", int(mask.sum()))


if __name__ == "__main__":
    main()
