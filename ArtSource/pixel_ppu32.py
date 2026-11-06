# 게임 그림을 1유닛 32px 밀도로 줄인다 (PixelOE k_centroid + 투명도 따로 자르기)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/pixel_ppu32.py [목록.json]
#   인자 없이: 아래 SPRITES(캐릭터, 폭탄). 목록을 주면 그 목록(소품, 바닥)을 줄이고 결과 크기를 _result.json에 쓴다
# 원본은 ArtSource/Original/ppu32/에 한 번만 백업하고, 다시 돌리면 그 백업에서 다시 줄인다.
# 줄인 뒤 Unity에서 PPU를 "옛 PPU x 줄인 비율"로 바꿔야 월드 크기가 그대로다(MCP로 함께 처리).
import os
import shutil
import sys

import cv2
import numpy as np
import torch
from PIL import Image
from pixeloe.torch.pixelize import pixelize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "_Project", "05. Art")
BACKUP = os.path.join(ROOT, "ArtSource", "Original", "ppu32")
TARGET = 32.0
PATCH = 4         # PixelOE 한 칸 크기. 입력을 출력 x PATCH로 맞춘 뒤 돌린다
THICKNESS = 1     # 외곽선 늘리기. 2부터는 작은 얼굴이 뭉개진다(11/6 시범)

# (그림, 방식, 값)
#   "density": 지금 화면 밀도(그림 px / 월드 유닛) = PPU / 쓰이는 배율. 32로 맞춘다
#   "extent":  코드가 크기를 맞추는 그림(폭탄). 긴 변을 이 px로
SPRITES = [
    ("Knight_hd.png", "density", 213.04 / 2.133),
    ("Archer_hd.png", "density", 171.74 / 1.717),
    ("Mantis_hd.png", "density", 184.78 / 1.484),
    ("Shielder_hd.png", "density", 213.04 / 2.133),
    ("Sniper_hd.png", "density", 195.65 / 1.96),
    ("Charger_hd.png", "density", 252.17 / 2.516),
    ("shielder_shield.png", "density", 100 / 1.935),
    ("Player3.png", "density", 100.0),
    ("player_die.png", "density", 100.0),
    ("golem1.png", "density", 300 / 0.65),   # 골렘 단계별 배율(GolemData _visualScale)
    ("golem2.png", "density", 300 / 0.96),
    ("golem3.png", "density", 300 / 1.0),
    ("arrow.png", "density", 100 / 0.8),
    ("bomb_balance.png", "extent", 0.86 * TARGET),   # Weapon_기본 _bombSize
    ("bomb_rapid.png", "extent", 0.9 * TARGET),
    ("bomb_huge.png", "extent", 1.25 * TARGET),
    ("UI/블랙홀폭탄.png", "density", 100 / 0.221),
]


# 외곽선 규칙(11/6 결정): 캐릭터와 소품 모두 검은 1px, 바닥과 이어 붙이는 울타리 조각은 없음.
# "tone"(자기 색을 어둡게)도 남겨 두어 목록에서 그림마다 고를 수 있다.
# 외곽선은 그림 안쪽 가장자리 1px에 칠해서 그림 크기와 배치가 바뀌지 않는다
INK = np.array([0.13, 0.09, 0.08])
TONE_DARKEN = 0.42      # 소품 외곽선 = 몸 색 x 이 값
TONE_MAX_LUM = 0.3      # 흰 석상처럼 밝은 소품도 외곽선이 이보다 밝지 않게


def lum(rgb):
    return rgb @ np.array([0.299, 0.587, 0.114])


def edge(mask):
    pad = np.pad(mask, 1)
    out = ~pad[:-2, 1:-1] | ~pad[2:, 1:-1] | ~pad[1:-1, :-2] | ~pad[1:-1, 2:]
    return mask & out


def body_color(rgb, targets, inside, pick="brightest"):
    # 가장자리 칸마다 5x5 안에서 외곽선이 아닌 몸 칸의 색. 외곽선 자리는 가장 밝은 색,
    # 테두리를 아예 지울 때(strip)는 가운데 밝기 색을 쓴다. 가장 밝은 색이면 이어 붙인 울타리 사이에 밝은 줄이 생긴다(11/6)
    h, w = targets.shape
    res = rgb.copy()
    for y, x in zip(*np.nonzero(targets)):
        y0, y1, x0, x1 = max(0, y - 2), min(h, y + 3), max(0, x - 2), min(w, x + 3)
        ok = inside[y0:y1, x0:x1]
        if ok.any():
            cand = rgb[y0:y1, x0:x1][ok]
            order = np.argsort(lum(cand))
            res[y, x] = cand[order[-1]] if pick == "brightest" else cand[order[len(order) // 2]]
    return res


DEEP_DEPTH = 4         # 소품은 원본 외곽선이 검정 → 진회색으로 4겹까지 번져 있어 넷째 겹까지 본다
DEEP_LUM = 0.40
ISLAND_PX = 6          # 이보다 작은 불투명 부스러기는 지운다(외곽선이 칠해져 바닥에 검은 점으로 남는다)


def near(m):
    pad = np.pad(m, 1)
    return pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]


def remove_islands(rgba):
    mask = (rgba[..., 3] > 0.5).astype(np.uint8)
    n, labels, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    small = np.isin(labels, [i for i in range(1, n) if stats[i, cv2.CC_STAT_AREA] < ISLAND_PX])
    out = rgba.copy()
    out[small, 3] = 0
    return out


def outline(rgba, style, deep=False):
    # style: "ink" 검은 1px, "tone" 자기 색 1px, "strip" 원래 있던 어두운 테두리까지 지워 외곽선 없음, "none" 손대지 않음
    if style == "none":
        return rgba
    rgb, mask = rgba[..., :3].copy(), rgba[..., 3] > 0.5
    ring1 = edge(mask)
    if not deep:
        # 캐릭터: 가장 바깥 1px만 칠하고 안쪽은 손대지 않는다. 둘째 겹을 몸 색으로 채우면
        # 손, 귀, 폭탄 사이 구분선까지 지워져 고블린 손과 귀가 붙어 보였다(11/6 사용자 지적, 시안 C)
        thick = np.zeros_like(mask)
    else:
        # 소품: 가장자리에서 안쪽으로 이어진 어두운 겹을 넷째 겹까지 지운다. 덜 지우면
        # "외곽선, 밝은 줄, 진회색 줄"로 줄이 하나 더 보인다(11/6 훈련 건물 아래)
        dark = lum(rgb) < DEEP_LUM
        thick = (ring1 & dark) if style == "strip" else np.zeros_like(mask)
        prev, rest = ring1, mask & ~ring1
        for _ in range(DEEP_DEPTH - 1):
            rk = edge(rest)
            rest &= ~rk
            cur = rk & dark & near(prev)
            if not cur.any():
                break
            thick |= cur
            prev = cur
    inside = mask & ~thick if style == "strip" else mask & ~ring1 & ~thick
    rgb = np.where(thick[..., None], body_color(rgb, thick, inside, "median" if style == "strip" else "brightest"), rgb)
    if style == "strip":
        pass
    elif style == "ink":
        rgb[ring1] = INK
    else:
        body = body_color(rgb, ring1, mask & ~ring1)[ring1]
        k = np.minimum(TONE_DARKEN, TONE_MAX_LUM / np.maximum(lum(body), 1e-4))
        rgb[ring1] = body * k[:, None]
    out = rgba.copy()
    out[..., :3] = rgb
    return out


def fill_transparent(rgba):
    # 투명한 곳을 가까운 불투명 색으로 채워, 외곽선 늘리기가 바깥 검은색을 끌어오지 않게 한다
    rgb = (rgba[..., :3] * 255).astype(np.uint8)
    mask = (rgba[..., 3] < 0.5).astype(np.uint8)
    return cv2.inpaint(rgb, mask, 3, cv2.INPAINT_TELEA) / 255.0


def pixelize_rgb(rgb, ow, oh):
    big = Image.fromarray((rgb * 255).astype(np.uint8), "RGB").resize((ow * PATCH, oh * PATCH), Image.LANCZOS)
    t = torch.from_numpy(np.asarray(big) / 255.0).float().permute(2, 0, 1)[None]
    with torch.no_grad():
        res = pixelize(t, pixel_size=PATCH, thickness=THICKNESS, mode="k_centroid", no_post_upscale=True, backend="torch")
    return res[0].permute(1, 2, 0).numpy().clip(0, 1)


def shrink(rgba, ow, oh):
    rgb = pixelize_rgb(fill_transparent(rgba), ow, oh)
    # 반투명 가장자리를 없애 경계가 딱 떨어지게 한다
    alpha = np.asarray(Image.fromarray((rgba[..., 3] * 255).astype(np.uint8)).resize((ow, oh), Image.BOX)) / 255.0
    return np.dstack([rgb, (alpha >= 0.5).astype(np.float32)])


def shrink_tile(rgba, ow, oh):
    # 반복 타일은 사방을 이어 붙인 뒤 줄이고 가운데만 잘라, 이음매에서 외곽선 늘리기가 끊기지 않게 한다
    h, w = rgba.shape[:2]
    big = np.tile(rgba[..., :3], (3, 3, 1))
    rgb = pixelize_rgb(big, ow * 3, oh * 3)[oh:oh * 2, ow:ow * 2]
    return np.dstack([rgb, np.ones((oh, ow), np.float32)])


def box_rgba(rgba, ow, oh):
    # 투명도를 곱한 채로 넓이 평균 → 다시 나눈다(가장자리 검은 번짐 방지)
    a = rgba[..., 3:4]
    pm = np.asarray(Image.fromarray((rgba[..., :3] * a * 255).astype(np.uint8), "RGB").resize((ow, oh), Image.BOX)) / 255.0
    al = np.asarray(Image.fromarray((a[..., 0] * 255).astype(np.uint8)).resize((ow, oh), Image.BOX)) / 255.0
    return (pm / np.maximum(al[..., None], 1e-4)).clip(0, 1), al


def shrink_box(rgba, ow, oh, tile=False):
    # 바닥처럼 얇은 줄이 많은 그림은 PixelOE가 줄을 점선으로 끊어(11/6 사용자 지적) 넓이 평균으로 줄인다.
    # 금, 바퀴 자국은 반투명이 핵심이라 50%로 자르지 않고 네 단계로만 남긴다
    if tile:
        big = np.tile(rgba, (3, 3, 1))
        rgb, al = box_rgba(big, ow * 3, oh * 3)
        rgb, al = rgb[oh:oh * 2, ow:ow * 2], al[oh:oh * 2, ow:ow * 2]
    else:
        rgb, al = box_rgba(rgba, ow, oh)
    al = np.round(al * 4) / 4
    return np.dstack([rgb, al])


def convert(rel, f, tile=False, method="oe", line="none"):
    dst = os.path.join(ART, rel)
    src = os.path.join(BACKUP, rel.replace("/", "_"))
    if not os.path.exists(src):
        shutil.copy2(dst, src)
    rgba = np.asarray(Image.open(src).convert("RGBA")) / 255.0
    h, w = rgba.shape[:2]
    ow, oh = max(1, round(w * f)), max(1, round(h * f))
    if method == "box":
        out = shrink_box(rgba, ow, oh, tile)
    else:
        out = shrink_tile(rgba, ow, oh) if tile else shrink(rgba, ow, oh)
    if line != "none":
        out = outline(remove_islands(out), line, deep=True)
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
    return w, h, ow, oh


def main_list(list_path):
    # Unity가 뽑은 목록(그림, 쓰임 밀도 중앙값, 타일 여부)을 받아 줄이고, 결과 크기를 같은 이름 _result.json에 쓴다
    import json
    os.makedirs(BACKUP, exist_ok=True)
    items = json.load(open(list_path, encoding="utf-8"))
    results = []
    for it in items:
        # 소품도 캐릭터처럼 검은 외곽선(11/6 사용자 결정), 타일과 넓이 평균(바닥) 그림은 외곽선 없음
        flat = it.get("tile", False) or it.get("method") == "box"
        line = it.get("outline", "none" if flat else "ink")
        w, h, ow, oh = convert(it["path"], TARGET / it["density"], it.get("tile", False), it.get("method", "oe"), line)
        results.append({"path": it["path"], "w": w, "h": h, "ow": ow, "oh": oh})
        print(f"{it['path']}\t{w}x{h}\t{ow}x{oh}")
    json.dump(results, open(list_path.replace(".json", "_result.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def main():
    if len(sys.argv) > 1:
        main_list(sys.argv[1])
        return
    os.makedirs(BACKUP, exist_ok=True)
    for rel, kind, value in SPRITES:
        dst = os.path.join(ART, rel)
        src = os.path.join(BACKUP, rel.replace("/", "_"))
        if not os.path.exists(src):
            shutil.copy2(dst, src)
        rgba = np.asarray(Image.open(src).convert("RGBA")) / 255.0
        h, w = rgba.shape[:2]
        f = TARGET / value if kind == "density" else value / max(w, h)
        ow, oh = max(1, round(w * f)), max(1, round(h * f))
        out = outline(shrink(rgba, ow, oh), "ink")
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        # Unity에서 쓸 비율: 세로 기준(발밑 기준점이 그대로 바닥에 닿게)
        print(f"{rel}\t{w}x{h}\t{ow}x{oh}\t{oh / h:.6f}")


if __name__ == "__main__":
    main()
