# 생성한 통짜 맵 바닥 그림을 맵 크기(2048px = 64유닛)에 맞추고 색 수를 줄여 픽셀 그림으로 만드는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_ground_pixelize.py [원본 이름] [--flatten-rims | --flatten-lawn-rims]
# 생성기가 가짜 점 없이 매끈하게 그려서(2000px), 격자를 재지 않고 1px = 게임 1px로 바로 맞춘 뒤 색만 나눈다.
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
SRC_DIR = ROOT / "ArtSource/Original/ground"
DST = ROOT / "Assets/_Project/05. Art/UI/맵 바닥.png"
SIZE = 2048     # 맵 바닥 64유닛 × 32px. 콘티를 이 크기로 찍어 배치가 그대로 맞는다
COLORS = 24     # 흙 얼룩, 자갈길, 돌 광장, 잔디 두 톤을 다 남기는 정도
SPECK = 3       # 색을 나눈 뒤 남는 한두 점짜리 잡티를 둘레 색으로 메우는 창 크기
PATH_LUM = (178, 190)   # 길 바탕색 밝기 범위(v3 원본 기준)
RIM_LUM = (155, 178)    # 길 가장자리 어두운 띠 밝기 범위. 이보다 어두운 자갈, 발자국은 남긴다
RIM_REACH = 31          # 길 바탕에서 이 폭(px) 안의 어두운 띠만 메운다
RIM_MIN_AREA = 700      # 이보다 작은 덩어리(발자국 한 짝 약 260px)는 띠가 아니라 남긴다
LAWN_PATH_LUM = 185     # 잔디 바탕(v4): 이보다 밝은 붉은 기 색은 길 바탕, 어두운 것은 길 가장자리 띠
BLADE_REACH = 45        # 잔디 바탕(v4): 길에서 이 폭 안의 짙은 풀잎을 밝은 풀색으로
LAWN_DARK_LUM = 158    # 잔디 바탕(v4): 이보다 어두운 초록은 길 윗가장자리 풀 띠(클로버도 같은 색이라 큰 덩어리만)


def flatten_path_rims(idx: np.ndarray, pal: np.ndarray) -> np.ndarray:
    # 생성기가 길 윗가장자리에 어두운 띠를 그려(프롬프트로 막아도) 길이 파인 홈에 그림자가 진 것처럼 보였다(사용자 지적, v3).
    # 길 바탕색 둘레 띠 안에 있는, 길보다 한 단계 어두운 색만 길 바탕색으로 메운다. 자갈, 발자국처럼 더 어두운 점은 둔다.
    lum = pal.astype(np.float32) @ np.array([0.299, 0.587, 0.114])
    counts = np.bincount(idx.ravel(), minlength=len(pal))
    fill = [i for i in range(len(pal)) if PATH_LUM[0] <= lum[i] < PATH_LUM[1]]
    rim = [i for i in range(len(pal)) if RIM_LUM[0] <= lum[i] < RIM_LUM[1]]
    if not fill or not rim:
        return idx
    main_fill = max(fill, key=lambda i: counts[i])
    near_path = cv2.dilate(np.isin(idx, fill).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (RIM_REACH, RIM_REACH))) > 0
    cand = (near_path & np.isin(idx, rim)).astype(np.uint8)
    # 발자국, 자갈도 같은 밝기라 함께 지워졌다. 길게 이어진 띠(넓은 덩어리)만 메우고 작은 점은 남긴다.
    n, lab, stats, _ = cv2.connectedComponentsWithStats(cand, 8)
    big = np.zeros(n, bool)
    big[1:] = stats[1:, cv2.CC_STAT_AREA] >= RIM_MIN_AREA
    idx[big[lab]] = main_fill
    return idx


def flatten_lawn_path_rims(idx: np.ndarray, pal: np.ndarray) -> np.ndarray:
    # 잔디 바탕(v4)도 생성기가 길 윗가장자리에 어두운 갈색 띠 + 짙은 초록 풀 띠를 그려 홈처럼 보였다(아랫가장자리는 밝은 풀).
    # 색조로 나눈다: 길은 붉은 기(r > g), 잔디는 초록 기(g > r). 길 둘레의 어두운 갈색 띠는 길 색으로 메우고,
    # 짙은 초록 풀잎은 모양은 두고 아랫가장자리 풀잎과 같은 밝은 풀색으로 바꿔 양쪽 가장자리를 똑같이 맞춘다.
    p = pal.astype(np.float32)
    lum = p @ np.array([0.299, 0.587, 0.114])
    warm = p[:, 0] > p[:, 1] + 15
    green = p[:, 1] > p[:, 0] + 5
    counts = np.bincount(idx.ravel(), minlength=len(pal))
    fill = [i for i in range(len(pal)) if warm[i] and lum[i] >= LAWN_PATH_LUM]
    rim = [i for i in range(len(pal)) if warm[i] and lum[i] < LAWN_PATH_LUM]
    # 길과 잔디 사이 1px 올리브 테두리(r ≈ g)도 같은 그림자 역할이라 함께 묶는다
    dark_green = [i for i in range(len(pal)) if not warm[i] and lum[i] < LAWN_DARK_LUM]
    lawn = [i for i in range(len(pal)) if green[i] and lum[i] >= LAWN_DARK_LUM]
    if not fill or not lawn:
        return idx
    main_fill = max(fill, key=lambda i: counts[i])
    light_blade = max(lawn, key=lambda i: lum[i])
    near_path = cv2.dilate(np.isin(idx, fill).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (RIM_REACH, RIM_REACH))) > 0
    cand = (near_path & np.isin(idx, rim)).astype(np.uint8)
    # 자갈, 발자국처럼 작은 점은 남기고 길게 이어진 띠만 메운다
    n, lab, stats, _ = cv2.connectedComponentsWithStats(cand, 8)
    big = np.zeros(n, bool)
    big[1:] = stats[1:, cv2.CC_STAT_AREA] >= RIM_MIN_AREA
    idx[big[lab]] = main_fill
    # 띠를 메운 뒤 넓어진 길에서 다시 잰다(띠 너머 풀잎까지 닿게 조금 더 넓게)
    near_path = cv2.dilate(np.isin(idx, fill).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (BLADE_REACH, BLADE_REACH))) > 0
    idx[near_path & np.isin(idx, dark_green)] = light_blade
    return idx


def main() -> None:
    name = sys.argv[1] if len(sys.argv) > 1 else "map_ground_v1.webp"
    img = Image.open(SRC_DIR / name).convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)
    # 색을 Lab 공간에서 나눈다. RGB 중간값 나누기는 넓이가 작은 회색 광장을 흙 갈색에 묶어 버렸다.
    a = np.asarray(img)
    lab = cv2.cvtColor(a, cv2.COLOR_RGB2LAB).reshape(-1, 3).astype(np.float32)
    rng = np.random.default_rng(0)
    sample = lab[rng.choice(len(lab), 80000, replace=False)]
    crit = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 50, 0.5)
    _, _, centers = cv2.kmeans(sample, COLORS, None, crit, 3, cv2.KMEANS_PP_CENTERS)
    label = np.empty(len(lab), np.int32)
    for i in range(0, len(lab), 1 << 20):
        chunk = lab[i:i + (1 << 20)]
        label[i:i + len(chunk)] = ((chunk[:, None, :] - centers[None]) ** 2).sum(-1).argmin(1)
    pal = cv2.cvtColor(centers.astype(np.uint8)[None], cv2.COLOR_LAB2RGB)[0]
    # 잡티는 다수결로 메운다(평균을 내지 않아 중간 색이 생기지 않는다)
    idx = np.asarray(Image.fromarray(label.reshape(SIZE, SIZE).astype(np.uint8)).filter(ImageFilter.ModeFilter(SPECK))).copy()
    # 흙 바탕(v3)의 길 가장자리 띠 메우기. 밝기 범위가 v3 원본에 맞춘 값이라 그때만 켠다.
    if "--flatten-rims" in sys.argv:
        idx = flatten_path_rims(idx, pal)
    if "--flatten-lawn-rims" in sys.argv:
        idx = flatten_lawn_path_rims(idx, pal)
    q = Image.fromarray(pal[idx])
    q.save(DST)
    a = np.asarray(q)
    print(DST.name, q.size, "colors", len(np.unique(a.reshape(-1, 3), axis=0)))


if __name__ == "__main__":
    main()
