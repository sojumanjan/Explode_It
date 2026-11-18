# 사용자가 맵 촬영 그림 위에 칠한 길, 광장, 잔디만 뽑아 배경을 지운 색 구역 그림(2048px)으로 만드는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_conti_extract.py [콘티 이름] [출력 이름] [뺄 구역...] [--lawn | --hedge | --muted]
#   뺄 구역: "구역@x0,y0,x1,y1"(콘티 그림 px). 가운데가 이 상자 안에 있는 그 구역 덩어리를 흙으로 둔다. 예: plaza@0,0,2000,2000
# 통짜 바닥 생성기에 배치 참조로 넣는다. 건물, 울타리가 남으면 생성기가 그것까지 그려서 바닥만 남긴다.
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
DIR = ROOT / "Docs/floor_mock"
SIZE = 2048          # 맵 바닥 64유닛 × 32px
TOL = 16             # 칠한 색과의 거리(webp 압축으로 가장자리가 조금 번짐)
MIN_AREA = 8000     # 이보다 작은 조각은 건물 벽 등 우연히 비슷한 색이라 버린다

# 사용자가 칠한 색 → 생성기에 보낼 구역 색
# v1은 길이 흙보다 훨씬 밝은 크림색이라 "대놓고 길"로 보였다(사용자). v2부터 길을 흙보다 조금 어둡게, 색차를 작게 둔다.
# v3는 v2를 넣어 보니 전체가 조금 어두워서(사용자) 한 단계 밝힌다.
PAINT = {
    "path": ((239, 229, 175), (168, 144, 108)),   # 밟혀 다져진 길. 흙과 같은 색 계열에 한 단계 어둡게
    "plaza": ((196, 196, 196), (160, 154, 144)),  # 돌 광장
    "grass": ((181, 230, 29), (96, 124, 70)),     # 조금 어두운 잔디
}
DIRT = (188, 162, 124)   # 나머지는 흙

# --lawn: 바탕을 잔디로 뒤집는다. 흙 바탕은 왕실 정원인데 폐허처럼 척박해 보였다(사용자, v4).
# 잔디는 울타리(짙은 초록)와 갈리고 초록 캐릭터(고블린, 궁수)가 묻히지 않게 밝고 노란 기, 낮은 채도. 칠한 잔디도 바탕과 같게 둔다.
LAWN = (140, 160, 94)
LAWN_PATH = (176, 152, 114)   # 잔디 위 흙길. 다져진 모래흙

# --hedge: v5 잔디가 노란 올리브라 풀 울타리(푸른 기 도는 초록)와 다른 식물처럼 따로 놀았다(사용자).
# 잔디를 울타리 팔레트의 밝은 쪽에서 채도만 낮춘 색으로 바꾸고, 울타리가 서는 자리와 그 둘레에 조금 짙은 풀 구역을 표시한다.
HEDGE_PAL = [(146, 189, 84), (124, 167, 77), (106, 149, 68), (91, 133, 64), (77, 118, 55), (62, 102, 47), (50, 85, 38), (38, 70, 30)]
HEDGE_LAWN = (148, 176, 102)
HEDGE_LAWN_PATH = (184, 160, 120)
HEDGE_BASE = (118, 150, 84)   # 울타리 밑동 둘레 조금 길고 짙은 풀
HEDGE_RING = 25               # 울타리 둘레로 넓히는 폭(px, 약 0.8유닛)
LAYOUT = DIR / "map_layout_raw.png"   # 울타리 자리를 찾는 맵 촬영 그림(2048px, 구역 그림과 같은 틀)

# --muted: 잔디는 너무 초록해서 오래 보면 눈이 피곤하다(사용자, v6 뒤). 전체 흙 바탕에 길만 살짝 어둡게, 채도 낮은 회갈색으로.
MUTED_DIRT = (170, 158, 136)
MUTED_PATH = (152, 140, 120)


def hedge_mask() -> np.ndarray:
    a = np.asarray(Image.open(LAYOUT).convert("RGB")).astype(np.int32)
    d = np.min([np.sqrt(((a - np.array(c)) ** 2).sum(-1)) for c in HEDGE_PAL], axis=0)
    m = (d < 10).astype(np.uint8)
    # 잎 질감 사이 빈틈을 메우고, 우연히 같은 색인 작은 점(나무, 정자 지붕 일부)은 버린다
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
    n, lab, stats, _ = cv2.connectedComponentsWithStats(m, 8)
    keep = np.zeros(n, bool)
    keep[1:] = stats[1:, cv2.CC_STAT_AREA] >= 3000
    m = keep[lab].astype(np.uint8)
    return cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (HEDGE_RING * 2 + 1, HEDGE_RING * 2 + 1))) > 0


def main() -> None:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    lawn = "--lawn" in sys.argv or "--hedge" in sys.argv
    hedge = "--hedge" in sys.argv
    global LAWN, LAWN_PATH, DIRT
    if hedge:
        LAWN, LAWN_PATH = HEDGE_LAWN, HEDGE_LAWN_PATH
    if "--muted" in sys.argv:
        # 길만 남기고 광장, 잔디는 모두 흙으로
        DIRT = MUTED_DIRT
        PAINT["path"] = (PAINT["path"][0], MUTED_PATH)
        PAINT.pop("plaza")
        PAINT.pop("grass")
    src = DIR / (args[0] if len(args) > 0 else "map_conti_user.webp")
    dst = DIR / (args[1] if len(args) > 1 else "map_conti_zones.png")
    drops = []
    for arg in args[2:]:
        name, box = arg.split("@")
        drops.append((name, tuple(int(v) for v in box.split(","))))
    if lawn:
        PAINT["path"] = (PAINT["path"][0], LAWN_PATH)
        PAINT["grass"] = (PAINT["grass"][0], LAWN)
    a = np.asarray(Image.open(src).convert("RGB")).astype(np.int32)
    out = np.empty_like(a)
    out[:] = LAWN if lawn else DIRT
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))
    for zname, (paint, zone) in PAINT.items():
        m = (np.sqrt(((a - np.array(paint)) ** 2).sum(-1)) < TOL).astype(np.uint8)
        # 붓 가장자리 번진 점과 작은 구멍을 메운다
        m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, kernel)
        # 탑 벽처럼 칠한 색과 우연히 같은 가는 조각은 붓 굵기(약 60px)보다 훨씬 가늘어 열기로 지운다
        m = cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 21)))
        n, lab, stats, cent = cv2.connectedComponentsWithStats(m, 8)
        keep = np.zeros(n, bool)
        keep[1:] = stats[1:, cv2.CC_STAT_AREA] >= MIN_AREA
        for dname, (x0, y0, x1, y1) in drops:
            if dname == zname:
                cx, cy = cent[:, 0], cent[:, 1]
                keep &= ~((cx >= x0) & (cx <= x1) & (cy >= y0) & (cy <= y1))
        out[keep[lab]] = zone
    img = Image.fromarray(out.astype(np.uint8)).resize((SIZE, SIZE), Image.NEAREST)
    if hedge:
        # 울타리 밑동 풀은 잔디 위에만(길 위에는 두지 않는다)
        o = np.asarray(img).copy()
        on_lawn = (o == np.array(LAWN)).all(-1)
        o[hedge_mask() & on_lawn] = HEDGE_BASE
        img = Image.fromarray(o)
    img.save(dst)
    print(dst.name, img.size)


if __name__ == "__main__":
    main()
