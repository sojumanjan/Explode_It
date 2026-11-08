# 사각 벽 그림체로 다시 뽑은 기울어진 울타리(흰 배경)를 지금 맵 울타리 자리와 크기에 맞춰 PPU 32 점 밀도, 64색으로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/hedge_v3_import.py
# 결과(그림 크기, 기준점, PPU)는 hedge_v3_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 칸을 다시 잡는다.
import json
import os
import shutil
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import building_import as bi
import pixel_ppu32 as pp

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "hedges_v3")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "hedges_v2_map")
RESULT = os.path.join(pp.ROOT, "ArtSource", "hedge_v3_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림, 지금 스프라이트 칸(텍스처 왼쪽 아래 기준 x, y, w, h), 기준점(칸 기준 px), PPU, 씬 배율(x, y).
# 울타리 4~7은 예전에 큰 그림에서 칸을 잘라 PPU와 배율을 제각각 맞춰 놓았다. 충돌 모양과 자리를 그대로 두려고
# 새 그림을 예전 그림이 실제로 차지하던 상자에 맞춰 넣고, PPU를 32 x 배율로 잡아 맵에서 점 밀도가 다른 소품(PPU 32, 1배)과 같게 한다.
TARGETS = [
    ("대각 곡선 가파름 왼쪽.png", "풀 울타리4.png", (94, 59, 191, 233), (95.5, 113.4), 74.6, (2.19, 2.33)),
    ("대각 곡선 완만 왼쪽.png", "풀 울타리5.png", (97, 108, 224, 214), (112.0, 106.25), 84.6, (2.64, 2.64)),
    ("대각 곡선 가파름 오른쪽.webp", "풀 울타리6.png", (83, 55, 179, 219), (89.5, 108.39), 70.0, (2.19, 2.19)),
    ("대각 곡선 완만 오른쪽.webp", "풀 울타리7.png", (102, 108, 224, 214), (112.0, 105.99), 84.6, (2.30, 2.64)),
    ("짧은 대각선.webp", "풀 울타리 9.png", (0, 0, 100, 79), (50.0, 12.0), 32.0, (1.5, 1.5)),
    ("긴 곡선.png", "풀 울타리 긴 곡선.png", (0, 0, 253, 67), (126.0, 8.0), 32.0, (1.0, 1.0)),
    # 아치는 맵의 사각 벽 그림을 참조로 넣어 같은 색과 음영으로 다시 뽑았다(11/8, 예전 아치는 중간 초록이 많아 벽 옆에서 짙어 보였다)
    ("아치 벽색.webp", "풀 아치1.png", (0, 0, 114, 132), (57.0, 16.0), 32.0, (1.37, 1.37)),
]
EDGE_ERODE = 2       # 흰 배경과 섞인 가장자리 점(원본 해상도 px)을 깎아 줄인 뒤 흰 테두리가 남지 않게 한다


def load_white_keyed(path):
    # 흰 배경 그림: 테두리에 이어진 밝은 무채색 칸만 배경이다. 흐린 잔상(글자 자국 등)은 가장 큰 덩어리만 남겨 지운다.
    src = Image.open(path).convert("RGBA")
    white = Image.new("RGBA", src.size, (255, 255, 255, 255))
    white.alpha_composite(src)
    rgb = np.asarray(white.convert("RGB")).astype(float)
    bright = (rgb.min(-1) > 200) & (rgb.max(-1) - rgb.min(-1) < 40)
    n, labels = cv2.connectedComponents(bright.astype(np.uint8), connectivity=4)
    border = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    fg = ~np.isin(labels, border[border > 0])
    n, labels, stats, _ = cv2.connectedComponentsWithStats(fg.astype(np.uint8), connectivity=4)
    fg = labels == 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])
    fg = cv2.erode(fg.astype(np.uint8), np.ones((3, 3), np.uint8), iterations=EDGE_ERODE) > 0
    ys, xs = np.nonzero(fg)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    return np.dstack([rgb[y0:y1, x0:x1] / 255.0, fg[y0:y1, x0:x1].astype(float)])


# 울타리끼리 겹치는 곳(구역 3 대각선 T자 등)에 각자의 짙은 테두리가 줄로 남아서(11/8 사용자 지적), 사각 벽처럼
# 테두리까지 그린 그림은 아래 층에, 바깥 RIM px를 깎은 몸통은 위 층에 그린다. 겹친 곳은 몸통끼리 덮여 하나로 이어진다.
RIM = 3


def body_of(rgba):
    body = rgba.copy()
    inner = body[..., 3] > 0.5
    for _ in range(RIM):
        pad = np.pad(inner, 1)
        inner &= pad[2:, 1:-1] & pad[:-2, 1:-1] & pad[1:-1, 2:] & pad[1:-1, :-2]
    body[..., 3] = inner
    return body


# 구역 2 아치 다리 바깥쪽은 좌우 사각 벽과 겹친다(맵 배치에서 계산한 값, 아치나 벽을 옮기면 다시 계산).
# - 벽 앞면 높이(front 줄): 다리 가장자리가 벽 앞면보다 짙어 꺾이는 곳에 세로 줄이 보이고 색이 끊겼다(11/9 사용자 지적).
#   다리 바깥 실루엣에서 blend px 안쪽까지, 바깥으로 갈수록 같은 높이 벽 앞면 색에 가깝게 점무늬로 섞는다.
# - 벽 윗면 높이(top 줄): 벽 몸통이 다리 테두리를 덮어 다리와 벽 윗면 사이 경계가 없어졌다(11/9 사용자 지적).
#   좌우 모두 몸통에 테두리까지 그대로 넣어, 벽 윗면 옆으로 접히는 곳까지 테두리가 보이게 한다.
# - 접히는 곳: 섞기가 한 줄에서 딱 끝나거나 접히는 줄의 밝은 색을 끌어오면 다리를 가로지르는 일자 줄이 생겼다(11/9 사용자 지적).
#   벽 색은 접히는 줄 아래 앞면 줄(fold_wall_row 이하)에서만 가져오고, 앞면 맨 위 fade 줄은 섞는 세기를 점점 줄인다.
# wall_bottom은 벽 바닥이 아치 그림에서 몇 번째 줄인지, wall_scale은 아치 1px당 벽 그림 px 수다.
LEG_BLEND = {"풀 아치1.png": {"front": (2, 56), "top": (56, 90), "wall_bottom": 2.2, "wall_scale": 1.205, "blend": 12,
                              "fold_wall_row": 60, "fade": 6}}
LEG_BLEND_WALL = os.path.join(bi.PROPS, "풀 울타리 벽 외곽.png")
BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 + 1 / 32.0


def blend_leg_edges(img, silhouette, cfg, rows):
    wall = np.asarray(Image.open(LEG_BLEND_WALL).convert("RGBA")).astype(float) / 255.0
    wh, ww = wall.shape[:2]
    mid = wall[:, ww // 4:ww * 3 // 4]
    # 벽과 아치가 함께 쓰는 초록 단계(어두운 것부터). 색을 섞을 때 이 단계 사이에서만 오간다.
    greens = np.unique(np.concatenate([wall[wall[..., 3] > 0.5][:, :3], img[img[..., 3] > 0.5][:, :3]]).round(4), axis=0)
    greens = greens[np.argsort(pp.lum(greens))]

    def step(c):
        return int(np.argmin(np.abs(greens - c.round(4)).sum(-1)))

    out = img.copy()
    h = out.shape[0]
    r0, r1 = rows
    for r in range(r0, r1):
        # 벽 앞면 같은 높이 줄의 가운데 칸에서 가장 흔한 색을 그 높이의 대표 색으로 쓴다
        wr = min(int(round((r - cfg["wall_bottom"]) * cfg["wall_scale"])), cfg["fold_wall_row"])
        px = mid[wh - 1 - wr][mid[wh - 1 - wr][:, 3] > 0.5][:, :3]
        colors, counts = np.unique(px.round(4), axis=0, return_counts=True)
        ref = step(colors[np.argmax(counts)])
        fade = min(1.0, (r1 - r) / cfg["fade"])
        y = h - 1 - r
        xs = np.nonzero(silhouette[y])[0]
        if len(xs) == 0:
            continue
        for x in range(xs.min(), xs.max() + 1):
            if out[y, x, 3] <= 0.5:
                continue
            d = min(x - xs.min(), xs.max() - x)
            t = (1.0 - d / cfg["blend"]) * fade
            if t <= 0:
                continue
            own = step(out[y, x, :3])
            v = own + (ref - own) * t
            k = int(np.floor(v)) + (1 if v - np.floor(v) > BAYER4[y % 4, x % 4] else 0)
            out[y, x, :3] = greens[min(max(k, 0), len(greens) - 1)]
    return out


def restore_rim(body, full, rows, right):
    # 한쪽 다리(right면 그림 가운데보다 오른쪽)만 몸통을 테두리 포함 전체 그림으로 바꾼다
    out = body.copy()
    h, w = out.shape[:2]
    cols = slice(w // 2, None) if right else slice(0, w // 2)
    out[h - rows[1]:h - rows[0], cols] = full[h - rows[1]:h - rows[0], cols]
    return out


def old_box(path, rect):
    # 예전 스프라이트 칸 안에서 그림이 실제로 차지하던 상자(칸 왼쪽 아래 기준 px)
    a = np.asarray(Image.open(path).convert("RGBA"))[..., 3] > 127
    th = a.shape[0]
    x, y, w, h = rect
    sub = a[th - y - h:th - y, x:x + w]
    ys, xs = np.nonzero(sub)
    return xs.min(), h - 1 - ys.max(), xs.max() + 1, h - ys.min()


def main():
    os.makedirs(BACKUP, exist_ok=True)
    palette = pp.load_palette()
    result = {}
    for src_name, dst_name, rect, pivot, ppu, (sx, sy) in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        bx0, by0, bx1, by1 = old_box(backup, rect)
        bw, bh = bx1 - bx0, by1 - by0
        # 맵에서 차지하는 크기(유닛)를 그대로 두고 PPU 32 x 배율에 맞춘 점 수. 배율이 가로세로 다르면 세로 점 수로 맞춘다.
        ow = round(bw / ppu * sx * 32)
        oh = round(bh / ppu * sy * 32 * sx / sy)
        rgba = load_white_keyed(os.path.join(SRC, src_name))
        out = pp.shrink(rgba, ow, oh)
        out = pp.apply_palette(out, palette)
        out = pp.remove_islands(out)
        body = body_of(out)
        if dst_name in LEG_BLEND:
            # 테두리 층도 같이 섞는다. 왼쪽 벽은 몸통 끝이 다리 가장자리에 못 미쳐 테두리 층이 드러난다.
            cfg = LEG_BLEND[dst_name]
            silhouette = out[..., 3] > 0.5
            body = restore_rim(body, out, cfg["top"], right=True)
            # 왼쪽 벽 몸통 끝과 아치 몸통 시작 사이로 테두리 층이 들쭉날쭉 줄처럼 비쳤다(11/9 사용자 지적).
            # 왼쪽 다리는 벽 높이 전체에서 몸통을 실루엣 끝까지 채운다. 끝 점은 아래에서 벽 색으로 섞인다.
            body = restore_rim(body, out, (cfg["front"][0], cfg["top"][1]), right=False)
            for layer in ("body", "out"):
                img = body if layer == "body" else out
                img = blend_leg_edges(img, silhouette, cfg, cfg["front"])
                if layer == "body":
                    body = img
                else:
                    out = img
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        Image.fromarray((body * 255).round().astype(np.uint8), "RGBA").save(dst[:-4] + " 몸통.png")
        piv = [(pivot[0] - bx0) / bw * ow, (pivot[1] - by0) / bh * oh]
        result[dst_name] = {"size": [ow, oh], "pivot": [round(piv[0], 2), round(piv[1], 2)], "ppu": round(32 * sx, 3)}
        aspect_new = rgba.shape[1] / rgba.shape[0]
        print(dst_name, (ow, oh), "pivot", result[dst_name]["pivot"], "ppu", result[dst_name]["ppu"],
              "aspect old %.2f new %.2f" % (bw * sx / (bh * sy), aspect_new))
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
