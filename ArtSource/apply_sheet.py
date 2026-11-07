# 생성 시트에서 캐릭터를 잘라 지금 게임 그림 자리에 끼운다(키, 발밑 위치, 기준점 유지 + 32px, 외곽선, 공통 팔레트)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/apply_sheet.py
# 결과로 Unity에서 바꿀 기준점을 apply_sheet_result.json에 쓴다(캔버스가 넓어진 그림만 x가 바뀐다).
# 여기 넣은 그림은 pixel_ppu32.py의 SPRITES에서 빼야 다시 돌릴 때 옛 그림으로 덮이지 않는다.
import json
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

ROOT = pp.ROOT
SHEETS = {
    "v1": os.path.join(ROOT, "ArtSource", "Original", "characters_sheet_v1.webp"),
    # v1의 사마귀(서 있는 곤충), 돌격병(오크), 방패(정면)를 다시 뽑은 것
    "v2": os.path.join(ROOT, "ArtSource", "Original", "characters_sheet_v2.png"),
    # 전사 한 장: v1은 칼을 내리고 있어 방패병과 헷갈려, 옛 전사 자세를 참조로 다시 뽑음
    "v3": os.path.join(ROOT, "ArtSource", "Original", "characters_sheet_v3.png"),
    # 궁수(활을 가슴 높이로), 방패병(철퇴)
    "v4": os.path.join(ROOT, "ArtSource", "Original", "characters_sheet_v4.png"),
    # 고블린 한 장: 32px에서 눈, 입이 뭉개져 얼굴을 크게 다시 뽑음(11/6). 마젠타 배경을 지운 것
    "v5": os.path.join(ROOT, "ArtSource", "Original", "characters_sheet_v5.png"),
}
# (시트, 시트 안 순서(왼쪽 위부터 줄 단위), 게임 그림, 뒤집기: "h" 좌우, "v" 위아래, None 그대로)
# 플레이어, 적은 프리팹의 _artFacesRight(옛 그림 방향)에 맞춰 진행 방향으로 뒤집으므로, 값을 바꾸는 대신
# 그림을 옛 방향에 맞춘다(11/6 사용자 확인). 방패는 코드가 그림 위쪽을 플레이어 쪽으로 돌려 바깥 면이 위여야 한다
TARGETS = [
    ("v5", 0, "Player3.png", None),
    ("v3", 0, "Knight_hd.png", None),    # 옛 자세를 따라 그려 방향도 옛 그림과 같다
    ("v4", 0, "Archer_hd.png", "h"),
    ("v4", 1, "Shielder_hd.png", "h"),
    ("v1", 5, "Sniper_hd.png", "h"),
    ("v2", 0, "Mantis_hd.png", "h"),
    ("v2", 1, "Charger_hd.png", "h"),
    ("v2", 2, "shielder_shield.png", "v"),
]
ROW_SPLIT = 450   # 시트에서 이 y보다 아래면 둘째 줄


def pieces(sheet):
    # 몸과 떨어진 무기 조각까지 한 덩어리로 묶되 옆 그림과는 붙지 않을 만큼만 부풀려, 그림마다 마스크를 만든다.
    # 칸 상자로 자르면 옆 칸 무기 끝이 같이 들어온다(11/6 돌격병 칸에 저격수 총 끝)
    fg = (sheet[..., 3] > 0.5).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(cv2.dilate(fg, np.ones((15, 15), np.uint8)), connectivity=8)
    ids = sorted([i for i in range(1, n) if st[i, 4] > 3000], key=lambda i: (st[i, 1] > ROW_SPLIT, st[i, 0]))
    out = []
    for i in ids:
        c = sheet.copy()
        c[..., 3] = ((lab == i) & (fg > 0)).astype(float)
        ys, xs = np.nonzero(c[..., 3])
        out.append(pp.remove_islands(c[ys.min():ys.max() + 1, xs.min():xs.max() + 1]))
    return out


def figure_box(alpha):
    ys, xs = np.nonzero(alpha > 0.5)
    return xs.min(), xs.max(), ys.min(), ys.max()


def main(only=None):
    pal = pp.load_palette()
    cache = {}
    results = []
    for sheet_key, index, rel, flip in TARGETS:
        if only and rel not in only:
            continue
        if sheet_key not in cache:
            cache[sheet_key] = pieces(np.asarray(Image.open(SHEETS[sheet_key]).convert("RGBA")) / 255.0)
        src = cache[sheet_key][index]
        dst = os.path.join(pp.ART, rel)
        cur = np.asarray(Image.open(dst).convert("RGBA")) / 255.0
        ch, cw = cur.shape[:2]
        x0, x1, y0, y1 = figure_box(cur[..., 3])
        fig_h = y1 - y0 + 1
        fig_w = max(1, round(src.shape[1] * fig_h / src.shape[0]))
        fig = pp.apply_palette(pp.outline(pp.remove_islands(pp.shrink(src, fig_w, fig_h)), "ink"), pal)
        # 그림만 제자리에서 뒤집어, 가운데와 기준점은 그대로 둔다(다시 돌려도 같은 결과)
        if flip == "h":
            fig = fig[:, ::-1]
        elif flip == "v":
            fig = fig[::-1]
        # 키와 발밑 줄은 지금 그림 그대로, 가로 가운데도 지금 그림 가운데에 맞춘다. 넓으면 캔버스를 양쪽으로 넓힌다
        nw = max(cw, fig_w + 2)
        shift = (nw - cw) // 2
        cx = (x0 + x1) / 2 + shift
        left = int(round(cx - (fig_w - 1) / 2))
        left = min(max(left, 0), nw - fig_w)
        canvas = np.zeros((ch, nw, 4))
        canvas[y1 - fig_h + 1:y1 + 1, left:left + fig_w] = fig
        pp.save(dst, canvas)
        results.append({"path": rel, "old_w": cw, "new_w": nw, "shift": shift})
        print(f"{rel}\t{cw}x{ch} -> {nw}x{ch}\tfigure {fig_w}x{fig_h}")
    # 일부만 다시 만들 때는 전체 기록을 덮지 않는다
    if not only:
        json.dump(results, open(os.path.join(ROOT, "ArtSource", "apply_sheet_result.json"), "w"), indent=1)


if __name__ == "__main__":
    # 그림 이름을 주면 그 그림만 다시 만든다. 예: apply_sheet.py Player3.png
    main(sys.argv[1:] or None)
