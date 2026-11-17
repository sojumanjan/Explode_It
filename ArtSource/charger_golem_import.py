# 잔묘사를 줄여 다시 뽑은 돌격병, 골렘 3단계를 원본 가짜 점 격자 그대로(점 하나 = 출력 한 점) 줄여 지금 그림 자리에 넣는다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/charger_golem_import.py [비교 그림 경로] [--apply]
# 생성 그림의 점 수가 게임 속 키(77, 46, 82, 98점)와 달라, 다시 늘리지 않고 원본 점 수대로 줄인 뒤 PPU로 게임 속 크기를 맞춘다.
# 원본에 이미 검은 1점 외곽선이 있어 외곽선을 더 두르지 않는다(두르면 왕처럼 두 겹이 된다).
# --apply 없이 돌리면 비교 그림만 만들고 게임 그림은 건드리지 않는다. 결과 크기와 PPU는 charger_golem_result.json에 적는다.
import json
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import boss_restyle_import as bi
import grid_pixelize as gp
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "charger_golem_v1")
COLORS = 24
CHROMA_W = 2.0
CORE = 0.6   # 칸 가운데 이만큼만 보고 다수결한다. 칸 가장자리는 옆 점과 섞인 흐린 색이다
# (원본, 덮어쓸 그림, 지금 그림의 PPU)
TARGETS = [
    ("charger_v1.webp", "Charger_hd.png", 80.90454),
    ("golem1_v1.png", "golem1.png", 20.658682),
    ("golem2_v1.webp", "golem2.png", 30.597015),
    ("golem3_v1.webp", "golem3.png", 32.09607),
]


def pitch_phase(ex, ey):
    # 색이 바뀌는 자리가 가장 규칙적으로 놓이는 점 간격과 시작 자리를 찾는다.
    # 점은 정사각형이라 가로세로를 한 간격으로 같이 찾는다(따로 찾으면 골렘 1단계 세로가 11.2로 잘못 잡혀 그림이 길쭉해졌다)
    best = (0, 0, 0, 0)
    ix, iy = np.arange(len(ex)), np.arange(len(ey))
    for p in np.arange(6.0, 26.0, 0.02):
        zx = (ex * np.exp(-2j * np.pi * ix / p)).sum() / ex.sum()
        zy = (ey * np.exp(-2j * np.pi * iy / p)).sum() / ey.sum()
        if abs(zx) + abs(zy) > best[0]:
            best = (abs(zx) + abs(zy), p, (np.angle(zx) / (2 * np.pi) * p) % p, (np.angle(zy) / (2 * np.pi) * p) % p)
    return best[1], best[2], best[3]


def lines(n, p, ph):
    # 그림 범위를 덮는 격자선. 범위 밖으로 반 칸 넘게 나간 선은 뺀다
    k0 = int(np.floor((0 - ph) / p))
    xs = [ph + k * p for k in range(k0, int((n - ph) / p) + 2)]
    xs = [x for x in xs if -p * 0.5 <= x <= n + p * 0.5]
    return np.array(xs)


def native(rgba):
    ys, xs = np.nonzero(rgba[..., 3] > 0.5)
    rgba = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    label, pal = gp.quantize(rgba, COLORS, CHROMA_W)
    rgb = np.where(rgba[..., 3:] > 0.5, rgba[..., :3], 1.0)
    ex = np.abs(np.diff(rgb, axis=1)).sum(2).sum(0)
    ey = np.abs(np.diff(rgb, axis=0)).sum(2).sum(1)
    px, phx, phy = pitch_phase(ex, ey)
    py = px
    # diff의 i번째 값은 i와 i+1 칸 사이 경계라 격자선은 +1
    gx, gy = lines(rgba.shape[1], px, phx + 1), lines(rgba.shape[0], py, phy + 1)
    out = np.full((len(gy) - 1, len(gx) - 1), -1)
    for j in range(len(gy) - 1):
        y0, y1 = gy[j], gy[j + 1]
        m = (y1 - y0) * (1 - CORE) / 2
        ya, yb = int(round(y0 + m)), int(round(y1 - m))
        for i in range(len(gx) - 1):
            x0, x1 = gx[i], gx[i + 1]
            n = (x1 - x0) * (1 - CORE) / 2
            cell = label[max(0, ya):max(0, yb), max(0, int(round(x0 + n))):max(0, int(round(x1 - n)))].ravel()
            if not len(cell):
                continue
            opaque = cell[cell >= 0]
            if len(opaque) >= 0.5 * len(cell):
                out[j, i] = np.bincount(opaque).argmax()
    fig = np.zeros(out.shape + (4,))
    m = out >= 0
    fig[m, :3] = pal[out[m]]
    fig[m, 3] = 1
    fig = pp.remove_islands(fig)
    ys, xs = np.nonzero(fig[..., 3] > 0.5)
    return fig[ys.min():ys.max() + 1, xs.min():xs.max() + 1], (px, py)


def to_image(a):
    return Image.fromarray((a * 255).round().astype(np.uint8), "RGBA")


def main():
    args = [a for a in sys.argv[1:] if a != "--apply"]
    apply = "--apply" in sys.argv
    compare = args[0] if args else None
    pairs, result = [], {}
    for src, rel, ppu in TARGETS:
        fig, (px, py) = native(bi.cut_out(os.path.join(SRC, src)))
        dst = os.path.join(pp.ART, rel)
        old = np.asarray(Image.open(dst).convert("RGBA")) / 255.0
        oys = np.nonzero((old[..., 3] > 0.5).any(1))[0]
        old_h = oys.max() - oys.min() + 1
        new_ppu = ppu * fig.shape[0] / old_h
        used = len(np.unique((fig[fig[..., 3] > 0.5][:, :3] * 255).round(), axis=0))
        print(rel, "pitch %.2f x %.2f" % (px, py), "old", old.shape[1::-1], "fig h", old_h, "-> new", fig.shape[1::-1], "ppu %.4f" % new_ppu, "colors", used)
        result[rel] = {"width": fig.shape[1], "height": fig.shape[0], "ppu": round(float(new_ppu), 5)}
        pairs.append((old, fig, old_h))
        if apply:
            to_image(fig).save(dst)
    json.dump(result, open(os.path.join(pp.ROOT, "ArtSource", "charger_golem_result.json"), "w"), indent=1)
    if compare:
        # 위 줄 지금 그림, 아래 줄 새 그림. 게임 속 키가 같아 보이게 새 그림 배율을 맞춘다(점 굵기 차이가 그대로 보인다)
        k = 6
        W = sum(max(o.shape[1] * k, int(f.shape[1] * k * h / f.shape[0]) + 1) for o, f, h in pairs) + 20 * (len(pairs) + 1)
        row = max(o.shape[0] for o, f, h in pairs) * k
        canvas = Image.new("RGBA", (W, row * 2 + 60), (200, 180, 140, 255))
        x = 20
        for o, f, h in pairs:
            a = to_image(o).resize((o.shape[1] * k, o.shape[0] * k), Image.NEAREST)
            s = h * k / f.shape[0]
            b = to_image(f).resize((int(f.shape[1] * s), h * k), Image.NEAREST)
            w = max(a.width, b.width)
            canvas.alpha_composite(a, (x + (w - a.width) // 2, 20 + row - a.height))
            canvas.alpha_composite(b, (x + (w - b.width) // 2, 40 + 2 * row - b.height))
            x += w + 20
        canvas.save(compare)


if __name__ == "__main__":
    main()
