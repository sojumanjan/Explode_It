# 게임 그림을 1유닛 32px 밀도로 줄인다 (PixelOE k_centroid + 투명도 따로 자르기)
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/pixel_ppu32.py [목록.json | --build-palette]
#   인자 없이: 아래 SPRITES(캐릭터, 폭탄). 목록을 주면 그 목록(소품, 바닥)을 줄이고 결과 크기를 _result.json에 쓴다
#   --build-palette: 전부 다시 뽑아 공통 팔레트(ArtSource/palette.json)를 새로 만들고 입힌다. 평소에는 있는 팔레트를 입힌다
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
# 고블린, 전사, 궁수, 방패병, 저격수는 생성 시트에서 새로 뽑아 apply_sheet.py가 넣는다(11/6). 여기 두면 옛 그림으로 덮인다
SPRITES = [
    ("Mantis_hd.png", "density", 184.78 / 1.484),
    ("Charger_hd.png", "density", 252.17 / 2.516),
    ("shielder_shield.png", "density", 100 / 1.935),
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


# 공통 팔레트(11/6 결정, 64색 + 채도 맞추기): 그림마다 팔레트가 달라 한 세계로 안 보이던 것을 맞춘다.
# 바닥 타일, 바닥 장식(넓이 평균 그림)은 넓은 면이 몇 단계로 뭉개져 얼룩이 생겨 뺀다
PALETTE_PATH = os.path.join(ROOT, "ArtSource", "palette.json")
PALETTE_SIZE = 64          # 외곽선 검정 포함
CHROMA_PULL = 0.5          # 그림 평균 채도를 전체 중앙값 쪽으로 당기는 정도(1이면 똑같이)
CHROMA_CLAMP = (0.7, 1.3)  # 한 그림의 채도 배율 한계
_M1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929], [0.2119034982, 0.6806995451, 0.1073969566], [0.0883024619, 0.2817188376, 0.6299787005]])
_M2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468], [1.9779984951, -2.4285922050, 0.4505937099], [0.0259040371, 0.7827717662, -0.8086757660]])


def oklab(rgb):
    lin = np.where(rgb <= 0.04045, rgb / 12.92, ((rgb + 0.055) / 1.055) ** 2.4)
    return np.cbrt(lin @ _M1.T) @ _M2.T


def from_oklab(lab):
    lin = ((lab @ np.linalg.inv(_M2).T) ** 3) @ np.linalg.inv(_M1).T
    return np.clip(np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.clip(lin, 0, None) ** (1 / 2.4) - 0.055), 0, 1)


def mean_chroma(rgba):
    lab = oklab(rgba[..., :3][rgba[..., 3] > 0.5])
    return float(np.hypot(lab[:, 1], lab[:, 2]).mean()) if len(lab) else 0.0


def harmonize(rgba, target):
    c = mean_chroma(rgba)
    if c <= 0:
        return rgba
    k = np.clip((target / c) ** CHROMA_PULL, *CHROMA_CLAMP)
    lab = oklab(rgba[..., :3].reshape(-1, 3))
    lab[:, 1:] *= k
    out = rgba.copy()
    out[..., :3] = from_oklab(lab).reshape(rgba.shape[:2] + (3,))
    out[np.all(np.abs(rgba[..., :3] - INK) < 0.02, -1), :3] = INK
    return out


def kmeans(x, k, iters=30, seed=0):
    rng = np.random.default_rng(seed)
    c = [x[rng.integers(len(x))]]
    for _ in range(k - 1):
        d = np.min(((x[:, None] - np.array(c)[None]) ** 2).sum(-1), 1)
        c.append(x[rng.choice(len(x), p=d / d.sum())])
    c = np.array(c)
    for _ in range(iters):
        lab = np.argmin(((x[:, None] - c[None]) ** 2).sum(-1), 1)
        for i in range(k):
            if (lab == i).any():
                c[i] = x[lab == i].mean(0)
    return c


def build_palette(rels):
    # 그림마다 같은 비중(최대 1500칸)으로 색을 뽑아, 큰 그림이 팔레트를 다 차지하지 않게 한다
    import json
    imgs = [np.asarray(Image.open(os.path.join(ART, r)).convert("RGBA")) / 255.0 for r in rels]
    target = float(np.median([c for c in map(mean_chroma, imgs) if c > 0]))
    rng = np.random.default_rng(1)
    samples = []
    for a in imgs:
        a = harmonize(a, target)
        px = a[a[..., 3] > 0.5][:, :3]
        px = px[~np.all(np.abs(px - INK) < 0.02, 1)]
        if len(px):
            samples.append(px[rng.choice(len(px), min(1500, len(px)), replace=False)])
    colors = np.vstack([from_oklab(kmeans(oklab(np.concatenate(samples)), PALETTE_SIZE - 1)), INK[None]])
    json.dump({"target_chroma": target, "colors": colors.round(5).tolist()}, open(PALETTE_PATH, "w"), indent=1)
    order = np.argsort(oklab(colors)[:, 0])
    swatch = np.repeat(np.repeat(colors[order][None], 24, 0), 24, 1)
    Image.fromarray((swatch * 255).round().astype(np.uint8)).save(PALETTE_PATH.replace(".json", ".png"))


def load_palette():
    import json
    if not os.path.exists(PALETTE_PATH):
        return None
    p = json.load(open(PALETTE_PATH))
    colors = np.array(p["colors"])
    return p["target_chroma"], colors, oklab(colors)


def apply_palette(rgba, palette):
    if palette is None:
        return rgba
    target, colors, lab_colors = palette
    a = harmonize(rgba, target)
    lab = oklab(a[..., :3].reshape(-1, 3))
    idx = np.argmin(((lab[:, None] - lab_colors[None]) ** 2).sum(-1), 1)
    out = a.copy()
    out[..., :3] = colors[idx].reshape(a.shape[:2] + (3,))
    return out


PALETTE = None   # main()에서 불러온다


def save(dst, out):
    Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)


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
    if method != "box":
        out = apply_palette(out, PALETTE)
    save(dst, out)
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
    global PALETTE
    if sys.argv[1:] == ["--build-palette"]:
        # 팔레트 없이 전부 다시 뽑고 → 그 결과로 팔레트를 만든 뒤 → 팔레트를 입혀 한 번 더 뽑는다
        import json
        props = os.path.join(ROOT, "ArtSource", "ppu32_props.json")
        main_sprites()
        main_list(props)
        rels = [r for r, _, _ in SPRITES] + [it["path"] for it in json.load(open(props, encoding="utf-8")) if it.get("method") != "box"]
        build_palette(rels)
        PALETTE = load_palette()
        main_sprites()
        main_list(props)
        return
    PALETTE = load_palette()
    if len(sys.argv) > 1:
        main_list(sys.argv[1])
        return
    main_sprites()


def main_sprites():
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
        out = apply_palette(outline(shrink(rgba, ow, oh), "ink"), PALETTE)
        save(dst, out)
        # Unity에서 쓸 비율: 세로 기준(발밑 기준점이 그대로 바닥에 닿게)
        print(f"{rel}\t{w}x{h}\t{ow}x{oh}\t{oh / h:.6f}")


if __name__ == "__main__":
    main()
