# 맵이 살아 있어 보이게 하는 반복 그림(분수대 물, 깃발 펄럭임, 촛불)과 떠다니는 장식(나뭇잎, 구름 그림자)을 지금 맵 그림에서 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_life_frames.py
# 생성 그림을 장마다 새로 뽑으면 돌 모양, 금 무늬가 장마다 달라 통째로 떨린다. 그래서 원래 그림은 그대로 두고 움직일 곳(물, 천, 불꽃)만 바꾼다.
# 결과는 Props/Life/에 원래 그림과 같은 크기로 쓰고, Unity에서 원래 스프라이트 칸과 기준점을 그대로 복사한다.
import math
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi

OUT = os.path.join(bi.PROPS, "Life")
WATER_FRAMES = 7  # 4장은 물결이 뚝뚝 끊겨 보여 늘림(11/9)
FLAG_FRAMES = 4
CANDLE_FRAMES = 3


def hexrgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def load(name):
    return np.asarray(Image.open(os.path.join(bi.PROPS, name)).convert("RGBA")).copy()


def save(arr, name):
    Image.fromarray(arr, "RGBA").save(os.path.join(OUT, name))
    print(name, arr.shape[1], arr.shape[0])


def color_mask(arr, hexes):
    m = np.zeros(arr.shape[:2], bool)
    for h in hexes:
        m |= np.all(arr[..., :3] == hexrgb(h), -1) & (arr[..., 3] > 127)
    return m


# 물: 가운데 물줄기는 무늬를 위로 흘려 보내고, 물 위 물결 색은 4단계로 밝아졌다 어두워지기를 반복한다.
# 한꺼번에 깜빡이면 수면 전체가 번쩍여 보여서, 물줄기에서 멀어질수록 단계를 늦춰 물결이 바깥으로 퍼지는 것처럼 보이게 한다.
WATER = {
    "분수대1_grid.png": {
        "blues": ["2f7184", "2f9bc3", "3fb3d8", "51c4e4", "66cee8", "83dbf1", "a5e5f9", "cbf5fd", "e6f9fd", "55a4b7"],
        # 물결 색이 오르내리는 밝기 단계(어두운 → 밝은). waves의 색만 이 단계에서 한 칸씩 밝아졌다 어두워진다.
        # 바탕 물색(51c4e4)과 테두리 어두운 색은 그대로 둔다.
        "ramp": ["3fb3d8", "51c4e4", "66cee8", "83dbf1", "a5e5f9", "cbf5fd", "e6f9fd"],
        "waves": ["3fb3d8", "66cee8", "83dbf1", "a5e5f9", "cbf5fd"],
        "ring": 7,  # 이 점 수마다 물결 단계가 한 장 늦는다
        "jet_step": 2,  # 한 장마다 물줄기 무늬가 올라가는 점 수(4장 때 3점 × 0.18초와 빠르기를 맞춤)
        "out": "분수대1_물",
    },
    "미니 음수대.png": {
        "blues": ["3790b5", "54c2e7", "a4e9f9"],
        "ramp": ["7fd6f0", "a4e9f9", "cbf5fd"],
        "waves": ["a4e9f9"],
        "ring": 4,
        "jet_step": 1,
        "out": "미니 음수대_물",
    },
}


def water_frames(name, cfg):
    src = load(name)
    blue = color_mask(src, cfg["blues"])
    h, w = blue.shape
    # 물줄기: 맨 위 물색 점들의 가로 범위를 물줄기 폭으로 본다. 그 폭 안에서 받침(크림색)이 처음 나오는 줄까지가 물줄기다.
    top = np.nonzero(blue.any(1))[0][0]
    band_rows = blue[top:top + max(4, h // 12)]
    xs = np.nonzero(band_rows.any(0))[0]
    jx0, jx1 = xs.min(), xs.max()
    cx = (jx0 + jx1) // 2
    opaque = src[..., 3] > 127
    y = top
    while y < h and (blue[y, cx] or not opaque[y, cx]):
        y += 1
    jet_bottom = y
    jet = np.zeros_like(blue)
    jet[:jet_bottom, jx0:jx1 + 1] = blue[:jet_bottom, jx0:jx1 + 1]
    # 물결 단계 기준점: 물줄기 밑. 위에서 내려다본 수면이라 세로를 두 배로 쳐 타원으로 퍼지게 한다.
    yy, xx = np.mgrid[0:h, 0:w]
    dist = np.sqrt((xx - cx) ** 2 + ((yy - jet_bottom) * 2.0) ** 2)
    lag = (dist // cfg["ring"]).astype(int)
    ramp = cfg["ramp"]
    ramp_cols = np.array([hexrgb(c) for c in ramp], np.uint8)
    # 장마다 밝기 단계를 사인 모양으로 한 칸 오르내린다(0, +1, +1, 0, 0, -1, -1). 4장일 때는 한 장마다 바로 바뀌어 깜빡였다.
    offsets = np.array([int(round(math.sin(2 * math.pi * f / WATER_FRAMES))) for f in range(WATER_FRAMES)])
    for f in range(WATER_FRAMES):
        out = src.copy()
        for base in cfg["waves"]:
            m = color_mask(src, [base]) & ~jet
            i = ramp.index(base) + offsets[(f - lag[m]) % WATER_FRAMES]
            # 단계 끝을 넘으면 반대로 꺾는다(가장 어두운 물결도 어두워질 차례에 밝아진다)
            i = np.where(i < 0, -i, i)
            i = np.where(i >= len(ramp), 2 * (len(ramp) - 1) - i, i)
            out[..., :3][m] = ramp_cols[i]
        # 물줄기: 열마다 물 점들의 색을 위로 굴린다(맨 위로 나간 색은 아래로 다시 들어온다)
        shift = f * cfg["jet_step"]
        for x in range(jx0, jx1 + 1):
            ys = np.nonzero(jet[:, x])[0]
            if len(ys) < 2:
                continue
            cols = src[ys, x, :3]
            out[ys, x, :3] = np.roll(cols, -shift, axis=0)
        save(out, f"{cfg['out']}_{f}.png")


# 깃발: 천 윗부분(고리)은 가로대에 걸려 있으니 두고, 아래로 갈수록 좌우로 크게 흔든다. 장마다 물결이 아래로 내려가게 줄마다 위상을 늦춘다.
# 기둥은 천 뒤에 가려져 있다가 꼬리 갈라진 틈과 천 아래로만 보이므로 그 열은 움직이지 않는다.
FLAG = {"src": "빨간 깃발1.png", "top": 25, "bottom": 121, "pole": (29, 33), "notch": 91, "amp": 1.5, "phase_per_row": 0.06, "out": "빨간 깃발1_펄럭"}


def flag_frames():
    src = load(FLAG["src"])
    h, w = src.shape[:2]
    p0, p1 = FLAG["pole"]
    for f in range(FLAG_FRAMES):
        out = src.copy()
        for y in range(FLAG["top"], FLAG["bottom"] + 1):
            ramp = (y - FLAG["top"]) / (FLAG["bottom"] - FLAG["top"])
            dx = int(round(FLAG["amp"] * ramp * math.sin(2 * math.pi * f / FLAG_FRAMES - (y - FLAG["top"]) * FLAG["phase_per_row"])))
            if dx == 0:
                continue
            row = src[y].copy()
            keep = np.zeros(w, bool)
            if y >= FLAG["notch"]:
                keep[p0:p1 + 1] = True
            moving = row.copy()
            moving[keep] = 0
            moved = np.zeros_like(row)
            if dx > 0:
                moved[dx:] = moving[:-dx]
            else:
                moved[:dx] = moving[-dx:]
            new = np.zeros_like(row)
            new[keep] = row[keep]
            put = moved[..., 3] > 0
            new[put] = moved[put]
            out[y] = new
        save(out, f"{FLAG['out']}_{f}.png")


# 촛불: 불꽃(심지 위 주황, 노랑 점)만 장마다 키를 줄이거나 밝기를 바꾼다. 세 초가 함께 깜빡이면 기계 같아 초마다 순서를 한 칸씩 어긋낸다.
CANDLE = {"src": "긴 촛대1.png", "wax": "fef7e2", "out": "긴 촛대1_불"}


def candle_frames():
    src = load(CANDLE["src"])
    h, w = src.shape[:2]
    wax = color_mask(src, [CANDLE["wax"]])
    opaque = src[..., 3] > 127
    # 초(흰 밀랍)를 세로 덩어리로 나눈다
    cols = np.nonzero(wax.any(0))[0]
    groups, start = [], cols[0]
    for a, b in zip(cols, cols[1:]):
        if b != a + 1:
            groups.append((start, a))
            start = b
    groups.append((start, cols[-1]))
    flames = []
    for x0, x1 in groups:
        wax_top = np.nonzero(wax[:, x0:x1 + 1].any(1))[0][0]
        # 밀랍 위로 이어진 점 중 외곽선(가장 어두운 색)이 아닌 것이 불꽃이다. 외곽선은 그대로 둔다.
        region = np.zeros_like(opaque)
        region[:wax_top, max(0, x0 - 1):x1 + 2] = opaque[:wax_top, max(0, x0 - 1):x1 + 2]
        lum = src[..., :3].astype(int).sum(-1)
        flame = region & (lum > 200)
        flames.append(flame)
    for f in range(CANDLE_FRAMES):
        out = src.copy()
        for i, flame in enumerate(flames):
            step = (f + i) % CANDLE_FRAMES
            ys, xs = np.nonzero(flame)
            if len(ys) == 0:
                continue
            if step == 1:
                # 불꽃 끝 한 줄을 지워 키가 줄어든다(지운 자리는 외곽선이 아니라 투명)
                top = ys.min()
                tip = flame & (np.arange(h)[:, None] == top)
                out[tip] = 0
            elif step == 2:
                # 불꽃 안쪽 색을 한 단계 밝게
                m = flame.copy()
                out[..., :3][m] = np.minimum(255, src[..., :3][m].astype(int) + 28).astype(np.uint8)
        save(out, f"{CANDLE['out']}_{f}.png")


# 나뭇잎: 2~3점짜리 잎이 뒤집히며 날아간다(넓은 면 → 옆면 → 넓은 면 반대). 맵에 벚나무가 없어 꽃잎 대신 나무1 잎 색(중간 연두, 그늘 초록)을 쓴다.
# 외곽선 없이 두 색만 쓴다(멀리 작게 날리는 것이라 외곽선이 있으면 점처럼 무겁다).
LEAF_LIGHT, LEAF_DARK = hexrgb("a7c74c"), hexrgb("7aa03f")


def leaf_frames():
    shapes = [
        ["LL.", "LDD"],
        [".L.", ".D."],
        [".LL", "DDL"],
    ]
    for i, rows in enumerate(shapes):
        arr = np.zeros((len(rows), len(rows[0]), 4), np.uint8)
        for y, row in enumerate(rows):
            for x, c in enumerate(row):
                if c == "L":
                    arr[y, x] = (*LEAF_LIGHT, 255)
                elif c == "D":
                    arr[y, x] = (*LEAF_DARK, 255)
        save(arr, f"나뭇잎_{i}.png")


# 구름 그림자: 원 몇 개를 겹친 덩어리. 가장자리 한 줄만 반투명으로 두 단계를 둬 픽셀 그림답게 끊어지게 한다. 진하기는 데이터의 색에서 정한다.
# 같은 모양이 여러 개 지나가면 복사한 티가 나서 모양을 5종 둔다(길쭉한 것, 둥근 것, 두 덩이로 갈라진 것 등).
CLOUD_SHAPES = [
    [(128, 74, 56), (78, 82, 40), (180, 80, 44), (110, 52, 36), (158, 56, 34), (52, 92, 26), (206, 94, 26)],
    [(128, 72, 50), (90, 70, 38), (166, 76, 40), (126, 46, 30)],
    [(70, 80, 34), (112, 66, 38), (150, 86, 30), (190, 72, 36), (226, 84, 22), (40, 90, 20)],
    [(84, 74, 40), (60, 88, 26), (112, 58, 28), (176, 80, 34), (206, 70, 24)],
    [(110, 78, 40), (150, 74, 36), (80, 88, 26), (184, 88, 26), (130, 56, 26)],
]


def cloud_shadow():
    w, h = 256, 144
    yy, xx = np.mgrid[0:h, 0:w]
    for i, blobs in enumerate(CLOUD_SHAPES):
        inside = np.full((h, w), -1e9)
        for cx, cy, r in blobs:
            d = np.sqrt((xx - cx) ** 2 + ((yy - cy) * 1.6) ** 2)
            inside = np.maximum(inside, r - d)
        core = inside > 4
        edge = (inside > 0) & ~core
        arr = np.zeros((h, w, 4), np.uint8)
        arr[core] = (0, 0, 0, 255)
        arr[edge] = (0, 0, 0, 128)
        # 4점 칸으로 눌러 큰 점 모양을 낸다(맵 점 크기에 맞춤)
        small = Image.fromarray(arr, "RGBA").resize((w // 4, h // 4), Image.NEAREST)
        save(np.asarray(small).copy(), f"구름 그림자_{i}.png")


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, cfg in WATER.items():
        water_frames(name, cfg)
    flag_frames()
    candle_frames()
    leaf_frames()
    cloud_shadow()


if __name__ == "__main__":
    main()
