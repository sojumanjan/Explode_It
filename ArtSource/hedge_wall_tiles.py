# 사각 울타리 벽 9-slice 그림을 생성 그림(톤만 바뀌는 매끈한 생울타리)에서 잘라 이음매 없게 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/hedge_wall_tiles.py
# 잎 모양을 찍은 무늬는 넓은 벽 면으로 이어지면 벌집처럼 보였고, 직접 그린 얼룩 무늬는 게임 분위기와 안 맞았다(11/8 사용자 지적).
# 사용자가 고른 생성 그림(잎 없이 톤만 바뀌고 테두리만 울퉁불퉁)을 PPU 32 높이로 줄여, 좌우 끝과 위아래 테두리는 그 픽셀을 그대로 쓰고
# 반복되는 가운데만 끝끼리 섞어 이음매를 없앤다. 잎 모양이 없어 섞어도 얼룩이 생기지 않는다.
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hedge_import as hi
import pixel_ppu32 as pp

SRC = os.path.join(hi.SRC, "네모 벽 매끈.png")
DST = os.path.join(pp.ART, "Props", "풀 울타리 벽.png")
# 벽끼리 겹치는 곳(세로 벽이 가로 벽에 붙는 곳)마다 각 벽의 짙은 테두리가 상대 윗면 위에 그어졌다(11/8 사용자 지적).
# 그래서 테두리까지 그린 그림(외곽)은 모든 벽 아래 층에 깔고, 위 층 몸통은 바깥 RIM px를 깎아 그린다. 겹친 곳은 몸통끼리 덮여 하나로 합쳐지고,
# 바깥 둘레에만 테두리가 남는다.
DST_OUTLINE = os.path.join(pp.ART, "Props", "풀 울타리 벽 외곽.png")
RIM = 3            # 몸통에서 깎아 낼 바깥 테두리 두께(px). 울퉁불퉁한 짙은 테두리가 1~3px라 이만큼 깎아야 겹친 곳에 남지 않는다

# 9-slice 나누기(px, PPU 32). 지금 블록(66x106)과 같은 크기라 가로 벽 한 줄은 늘리지 않아도 이 크기 그대로다.
SIDE = 17          # 좌우 끝(늘지 않음). 66 - 17*2 = 32라 블록 하나 폭이면 가운데가 정확히 한 칸이다
TOP = 16           # 위 테두리(늘지 않음). 윗면 위쪽 둥근 모서리 줄(좌우가 안쪽으로 말린 줄)을 모두 여기에 넣는다
TILE = 32          # 가로 반복 칸
TILE_Y = 24        # 세로 반복 칸. 윗면에서 좌우가 곧은 줄이 26줄뿐이라 32줄로 잡으면 둥근 모서리 줄이 섞여 세로 벽이 알약을 쌓은 모양이 됐다
FRONT = 66         # 앞면(늘지 않음). 밝은 모서리 줄부터 시작한다
HEIGHT = TOP + TILE_Y + FRONT
BLEND_X = 12       # 가로 반복 이음매를 섞는 px
BLEND_Y = 2        # 세로 반복 이음매를 섞는 px. 좌우가 곧은 윗면 줄(26줄) 안에서 TILE_Y + 2줄을 떼어 온다
BAND_X = 0.45      # 반복 칸을 떼어 올 가로 위치(그림 폭 비율). 좌우 끝 그늘이 섞이지 않는 가운데쯤


def load():
    a = np.asarray(Image.open(SRC).convert("RGBA")).astype(float) / 255.0
    ys, xs = np.nonzero(a[..., 3] > 0.5)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
    a[..., 3] = a[..., 3] > 0.5
    return a


# 윗면: 생성 그림의 다이아몬드 무늬가 32x24px마다 똑같이 반복돼 바둑판처럼 부자연스러웠다(11/8 사용자 지적).
# 테두리 몇 px만 원래 픽셀로 두고, 안쪽은 윗면에 쓰인 초록 세 단계로 무늬 없는 반복 얼룩을 디더링해 다시 칠한다.
TOP_RIM = 2         # 바깥 가장자리에서 이 px까지는 원래 그림(울퉁불퉁한 짙은 테두리)을 그대로 둔다
TOP_BLUR = 1.0      # 얼룩 크기(px). 3px로 키우면 칸(32x24)마다 같은 덩어리 모양이 반복돼 보여, 벽면처럼 잘고 고른 결로 둔다
TOP_DITHER = 0.5    # 단계 경계를 섞는 디더링 세기(0이면 딱 끊김)
SEED = 3
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 - 0.47


def wrap_blur(field, sigma):
    # 위아래, 좌우가 이어지는 흐림(FFT). 반복 칸 끝끼리 얼룩이 이어진다.
    h, w = field.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    kernel = np.exp(-2 * (np.pi * sigma) ** 2 * (fx ** 2 + fy ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(field) * kernel))


def top_tones(out):
    # 윗면 반복 칸에 쓰인 색 중 가장 많은 색을 가운데로, 그 바로 어둡고 밝은 색을 양쪽으로
    block = out[TOP:TOP + TILE_Y, SIDE:SIDE + TILE]
    rgb = block[..., :3][block[..., 3] > 0.5]
    uniq, count = np.unique(np.round(rgb, 4), axis=0, return_counts=True)
    keep = count >= 8
    uniq, count = uniq[keep], count[keep]
    order = np.argsort(pp.lum(uniq))
    uniq, count = uniq[order], count[order]
    mid = int(count.argmax())
    lo, hi = max(0, mid - 1), min(len(uniq) - 1, mid + 1)
    return np.stack([uniq[lo], uniq[mid], uniq[hi]])


def repaint_top(out):
    tones = top_tones(out)
    rng = np.random.default_rng(SEED)
    field = wrap_blur(rng.random((TILE_Y, TILE)), TOP_BLUR)
    rank = np.argsort(np.argsort(field.ravel())).reshape(field.shape) / field.size
    dither = np.tile(BAYER, (TILE_Y // 4, TILE // 4)) * TOP_DITHER
    # 가운데 색이 대부분 덮도록 순위 0~1을 0~3 단계로 나눈다(어두움 15%, 가운데 70%, 밝음 15%)
    level = np.clip(np.floor(np.interp(rank, [0, 0.15, 0.85, 1], [0, 1, 2, 3]) + dither), 0, 2).astype(int)
    tex = tones[level]

    alpha = out[..., 3] > 0.5
    inner = alpha.copy()
    for _ in range(TOP_RIM):
        pad = np.pad(inner, 1)
        inner &= pad[2:, 1:-1] & pad[:-2, 1:-1] & pad[1:-1, 2:] & pad[1:-1, :-2]
    for y in range(TOP + TILE_Y):
        for x in range(out.shape[1]):
            if inner[y, x]:
                out[y, x, :3] = tex[(y - TOP) % TILE_Y, (x - SIDE) % TILE]
    return out


def soften_edge(out):
    # 윗면과 앞면이 만나는 각: 생성 그림의 밝은 노란 줄 두 줄이 튀어 보여(11/8 사용자 지적), 윗면 색에서 앞면 색으로
    # 한 단계씩 어두워지는 세 줄로 바꾼다. 단계 사이는 체크무늬로 섞는다.
    edge = TOP + TILE_Y
    alpha = out[..., 3] > 0.5
    colors = out[..., :3][alpha]
    uniq, count = np.unique(np.round(colors, 4), axis=0, return_counts=True)
    top_mid = top_tones(out)[1]
    body = out[edge + 4:edge + 14, SIDE:SIDE + TILE]
    b_uniq, b_count = np.unique(np.round(body[..., :3][body[..., 3] > 0.5], 4), axis=0, return_counts=True)
    front_mid = b_uniq[b_count.argmax()]
    # 윗면 가운데 색과 앞면 가운데 색 사이 밝기의 색(그림에 이미 있는 팔레트 색)
    lo, hi = pp.lum(front_mid[None])[0], pp.lum(top_mid[None])[0]
    between = [c for c in uniq if lo < pp.lum(c[None])[0] < hi]
    step = min(between, key=lambda c: abs(pp.lum(c[None])[0] - (lo + hi) / 2)) if between else front_mid
    rows = [(top_mid, step), (step, step), (step, front_mid)]
    for k, (a, b) in enumerate(rows):
        y = edge + k
        for x in range(out.shape[1]):
            if alpha[y, x]:
                out[y, x, :3] = a if (x + y) % 2 else b
    return out


def main():
    palette = pp.load_palette()
    src = load()
    width = round(HEIGHT * src.shape[1] / src.shape[0])
    img = pp.shrink(src, width, HEIGHT)
    img[..., 3] = img[..., 3] > 0.5

    # 줄 배치: 위 테두리 = 맨 위 TOP줄, 반복 칸 = 윗면 안쪽 줄, 앞면 = 밝은 모서리 줄부터 FRONT줄.
    # 밝은 모서리 줄이 반복 칸 안에 들면 세로 벽에서 32px마다 밝은 줄이 생겨, 앞면 첫 줄부터 시작하게 맞춘다.
    lum = pp.lum(img[:, width // 3:2 * width // 3, :3]).mean(1)
    edge = int(np.argmax(lum[:HEIGHT // 2]))           # 밝은 모서리 줄(두 줄이면 위쪽)
    if lum[edge - 1] > lum[edge] * 0.95:
        edge -= 1
    front = img[edge:edge + FRONT].copy()
    if len(front) < FRONT:
        front = np.concatenate([front, np.zeros((FRONT - len(front),) + front.shape[1:])], 0)
    # 밝은 모서리 줄은 몸통보다 좌우로 1~3px 튀어나와 있어, 바로 아래 몸통 폭에 맞춰 자른다
    for r in range(2):
        front[r, ..., 3] *= front[3, ..., 3]

    # 세로 반복: 윗면 안쪽 줄(둥근 위 모서리 아래부터 밝은 줄 위까지)에서 TILE + BLEND_Y줄을 떼어,
    # 칸 첫 BLEND_Y줄에 아래로 넘친 줄을 점점 섞는다. 칸 바로 위 줄과 섞으면 둥근 모서리의 빈칸을 끌어와 세로 벽 좌우에 32px마다 홈이 파였다.
    start = edge - 1 - TILE_Y - BLEND_Y
    strip = img[start:edge - 1].copy()
    tile_rows = strip[:TILE_Y].copy()
    for j in range(BLEND_Y):
        t = (j + 0.5) / BLEND_Y
        tile_rows[j] = t * strip[j] + (1 - t) * strip[TILE_Y + j]
    img = np.concatenate([img[:TOP], tile_rows, front], 0)

    # 가로 반복: 가운데에서 TILE + BLEND_X 폭을 떼어, 왼쪽 BLEND_X 열에 오른쪽으로 넘친 열을 점점 섞는다
    x0 = int(width * BAND_X)
    band = img[:, x0:x0 + TILE + BLEND_X]
    center = band[:, :TILE].copy()
    for i in range(BLEND_X):
        t = (i + 0.5) / BLEND_X
        center[:, i] = t * band[:, i] + (1 - t) * band[:, TILE + i]

    out = np.concatenate([img[:, :SIDE], center, img[:, width - SIDE:]], 1)
    out[..., 3] = out[..., 3] > 0.5
    out = pp.apply_palette(out, palette)
    out = repaint_top(out)
    out = soften_edge(out)
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(DST_OUTLINE)
    body = out.copy()
    inner = body[..., 3] > 0.5
    for _ in range(RIM):
        pad = np.pad(inner, 1)
        inner &= pad[2:, 1:-1] & pad[:-2, 1:-1] & pad[1:-1, 2:] & pad[1:-1, :-2]
    body[..., 3] = inner
    Image.fromarray((body * 255).round().astype(np.uint8), "RGBA").save(DST)
    print("풀 울타리 벽.png", out.shape[1::-1], "from", (width, HEIGHT), "border(LBRT)", [SIDE, FRONT, SIDE, TOP])


if __name__ == "__main__":
    main()
