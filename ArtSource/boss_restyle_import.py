# 맵 그림체로 다시 뽑은 보스 4종(흰 배경, 점 하나가 약 8~10px)을 게임 속 키(1유닛 32점)로 줄여 지금 보스 그림 자리에 넣는다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/boss_restyle_import.py [비교 그림 경로]
# 생성 그림 점 격자는 반듯하지 않아 격자를 재지 않고, 프롬프트에 박은 키(점 수)로 칸 다수결을 한다(grid_pixelize).
# 배경은 테두리에서 이어진 흰색만 지운다. 외곽선 안쪽의 흰 머리카락, 흰 털은 남는다.
# 지금 그림의 캔버스 크기, 발밑 줄, 가운데를 그대로 써서 PPU, 기준점, 프리팹 배율을 안 바꾼다. 폭이 모자라면 캔버스만 좌우로 넓힌다(기준점이 아래 가운데).
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import grid_pixelize as gp
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "boss_restyle")
COLORS = 24
CHROMA_W = 2.0    # 1.0이면 연한 피부색이 흰 머리카락에 묶여 얼굴이 하얗게 날아간다(11/10)
BG_MIN = 222      # 세 채널이 모두 이 값 이상이고
BG_SPREAD = 28    # 채널 차이가 이 값 이하면 흰 배경 후보
# (원본, 시트 안 몇 번째 덩어리(왼쪽부터), 게임 속 키(점), 덮어쓸 그림)
TARGETS = [
    ("fire_mage_v3.png", 0, 70, "boss/Fire_Mage/fire_mage.png"),
    ("miner_v1.webp", 0, 71, "boss/Miner/miner_walk.png"),
    ("beast_wonhon_v1.webp", 0, 68, "boss/Beast/beast_move.png"),
    ("beast_wonhon_v1.webp", 1, 39, "boss/Beast/beast_fire.png"),
]
# 각성한 왕은 "각성해서 돌변한" 연출로 예전 날 선 그림체를 그대로 둔다(11/10 결정). king_v1.webp는 원본만 보관
# 한 시트에 자세 여러 개: (원본, 기준 덩어리, 기준 키(점), [(덩어리, 덮어쓸 그림), ...]).
# 기준 덩어리를 키에 맞춘 배율을 시트 전체에 그대로 써서, 누운 자세도 서 있는 자세와 캐릭터 크기가 같다
SHEETS = [
    # 광부 그로기는 머리 위 별까지 키에 들어가, 별을 뺀 몸이 걷기 그림(71)과 같아지게 84로 둔다
    ("miner_groggy_die_v1.webp", 0, 84, [(0, "boss/Miner/miner_groggy.png"), (1, "boss/Miner/miner_die.png")]),
    ("beast_groggy_die_v1.webp", 0, 61, [(0, "boss/Beast/beast_groggy.png"), (1, "boss/Beast/beast_die.png")]),
]
# 변환 뒤 사용자가 얼굴을 직접 찍은 그림. 다시 돌려도 덮어쓰지 않는다(11/10)
HAND_EDITED = {"boss/Fire_Mage/fire_mage.png", "boss/Beast/beast_move.png", "boss/Beast/beast_fire.png"}


def cut_out(path):
    im = Image.open(path)
    if im.mode == "RGBA":
        # 배경이 이미 투명하게 온 그림(마법사, 광부, 왕)은 투명도를 그대로 쓴다. 투명 칸 RGB는 검정이라 흰색 찾기로는 못 지운다
        rgba = np.asarray(im) / 255.0
        if (rgba[..., 3] < 0.5).mean() > 0.2:
            rgba = rgba.copy()
            rgba[..., 3] = (rgba[..., 3] > 0.5).astype(float)
            return rgba
    a = np.asarray(im.convert("RGB")).astype(np.int32)
    white = (a.min(-1) >= BG_MIN) & (a.max(-1) - a.min(-1) <= BG_SPREAD)
    # 마젠타 배경으로 온 시트(야수 그로기, 사망)도 같이 지운다
    magenta = (a[..., 0] > 150) & (a[..., 2] > 150) & (a[..., 1] < 110)
    n, lab = cv2.connectedComponents((white | magenta).astype(np.uint8), connectivity=4)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border > 0])
    return np.dstack([a / 255.0, (~bg).astype(float)])


def pieces(rgba):
    # 시트 안 그림을 세로 빈 열로 나눈다(불씨 같은 작은 조각은 옆 그림에 붙인다)
    col = (rgba[..., 3] > 0.5).sum(0)
    xs = np.nonzero(col > 0)[0]
    runs, s = [], xs[0]
    for p, q in zip(xs[:-1], xs[1:]):
        if q - p > 20:
            runs.append((s, p + 1)); s = q
    runs.append((s, xs[-1] + 1))
    runs = [r for r in runs if r[1] - r[0] > 60] or runs
    out = []
    for x0, x1 in runs:
        part = rgba[:, x0:x1]
        ys = np.nonzero((part[..., 3] > 0.5).any(1))[0]
        out.append(part[ys[0]:ys[-1] + 1])
    return out


def pixelize(rgba, oh, ow=None):
    # 눈, 입 같은 1점 표정은 다수결에서 사라진다. 어두운 점을 우선 고르게 해 봤지만 흐릿하게만 남아 빼고,
    # 표정은 사용자가 직접 찍거나 원본을 그대로 넣기로 함(11/10)
    h, w = rgba.shape[:2]
    if ow is None:
        ow = max(1, round(w * oh / h))
    label, pal = gp.quantize(rgba, COLORS, CHROMA_W)
    small = gp.drop_specks(gp.mode_downsample(label, ow, oh, gp.ALPHA_MIN))
    out = np.zeros((oh, ow, 4))
    m = small >= 0
    out[m, :3] = pal[small[m]]
    out[m, 3] = 1
    return pp.outline(pp.remove_islands(out), "ink", deep=False)


def place(fig, old):
    # 지금 그림의 발밑 줄과 가로 가운데에 맞춘다
    ch, cw = old.shape[:2]
    ys, xs = np.nonzero(old[..., 3] > 0.5)
    bottom = ys.max()
    center = (xs.min() + xs.max()) / 2
    fh, fw = fig.shape[:2]
    pad = max(0, fw + 2 - cw)
    pad += pad % 2
    # 키가 캔버스를 넘으면 위로만 늘린다(기준점이 아래 가운데라 발밑 자리는 그대로)
    grow = max(0, fh + 1 - (bottom + 1))
    canvas = np.zeros((ch + grow, cw + pad, 4))
    bottom += grow
    center += pad // 2
    left = int(np.clip(round(center - (fw - 1) / 2), 0, canvas.shape[1] - fw))
    top = bottom - fh + 1
    src_top = max(0, -top)
    canvas[max(0, top):bottom + 1, left:left + fw] = fig[src_top:]
    return canvas


def main():
    compare = sys.argv[1] if len(sys.argv) > 1 else None
    pairs = []
    cache = {}
    jobs = []
    for src, idx, oh, rel in TARGETS:
        if src not in cache:
            cache[src] = pieces(cut_out(os.path.join(SRC, src)))
        jobs.append((rel, cache[src][idx], oh, None))
    for src, ref, ref_h, items in SHEETS:
        parts = pieces(cut_out(os.path.join(SRC, src)))
        k = ref_h / parts[ref].shape[0]
        for idx, rel in items:
            h, w = parts[idx].shape[:2]
            jobs.append((rel, parts[idx], max(1, round(h * k)), max(1, round(w * k))))
    for rel, part, oh, ow in jobs:
        if rel in HAND_EDITED:
            print(rel, "손질본이라 건너뜀")
            continue
        fig = pixelize(part, oh, ow)
        dst = os.path.join(pp.ART, rel)
        old = np.asarray(Image.open(dst).convert("RGBA")) / 255.0
        new = place(fig, old)
        Image.fromarray((new * 255).round().astype(np.uint8), "RGBA").save(dst)
        used = len(np.unique((new[new[..., 3] > 0.5][:, :3] * 255).round(), axis=0))
        print(rel, "old", old.shape[1::-1], "new", new.shape[1::-1], "fig", fig.shape[1::-1], "colors", used)
        pairs.append((old, new))
    if compare:
        # 위 줄 지금 그림, 아래 줄 새 그림. 게임 속 점 크기가 같으므로 같은 배율로 키운다
        k = 5
        cells = [(max(o.shape[1], n.shape[1]), max(o.shape[0], n.shape[0])) for o, n in pairs]
        W = sum(c[0] for c in cells) * k + 20 * (len(cells) + 1)
        H = sum(max(c[1] for c in cells) for _ in range(2)) * k + 60
        canvas = Image.new("RGBA", (W, H), (126, 160, 80, 255))
        x = 20
        row_h = max(c[1] for c in cells) * k
        for (o, n), (cw, chh) in zip(pairs, cells):
            for r, img in enumerate((o, n)):
                im = Image.fromarray((img * 255).round().astype(np.uint8), "RGBA")
                im = im.resize((im.width * k, im.height * k), Image.NEAREST)
                canvas.alpha_composite(im, (x + (cw * k - im.width) // 2, 20 + r * (row_h + 20) + row_h - im.height))
            x += cw * k + 20
        canvas.save(compare)


if __name__ == "__main__":
    main()
