# 보스, 왕 그림을 1유닛 32px로 바꾼다(보스 전용 팔레트 + 캐릭터 몸통 외곽에만 검은 외곽선) + 왕 표정 컷인 초상화를 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/apply_bosses.py
# 대상과 쓰임 밀도는 ppu32_bosses.json(Unity에서 뽑음). 원본은 ArtSource/Original/ppu32/에 한 번만 백업하고 늘 백업에서 다시 만든다.
# 2등신 왕 9장은 새 시트(king_chibi_sheet_v1.webp)를 옛 300px 칸(키, 발밑, 가운데)에 맞춰 끼운 것을 원본으로 쓴다.
# 결과 크기는 ppu32_bosses_result.json에 쓰고, Unity에서 PPU를 "옛 PPU x 줄인 비율"로 바꿔 월드 크기를 유지한다.
import json
import os
import shutil
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

ROOT = pp.ROOT
LIST = os.path.join(ROOT, "ArtSource", "ppu32_bosses.json")
PALETTE = os.path.join(ROOT, "ArtSource", "palette_boss.json")
PORTRAIT_DIR = os.path.join(pp.ART, "King", "Portrait")
KING_SHEET = os.path.join(ROOT, "ArtSource", "Original", "king_chibi_sheet_v1.webp")
# 왕 시트 칸: (이름, 줄 y범위, x범위). 줄마다 세로 빈 열로 나눈 값
KING_CELLS = [
    ("king_smug", (0, 527), (31, 271)), ("king_lazy", (0, 527), (298, 600)), ("king_laugh", (0, 527), (607, 940)),
    ("king_letsgo", (0, 527), (955, 1228)), ("king_shock", (0, 527), (1242, 1524)),
    ("king_speechless", (527, 1024), (22, 314)), ("king_embarrassed", (527, 1024), (363, 672)),
    ("king_cry", (527, 1024), (688, 1061)), ("king_rage", (527, 1024), (1093, 1415)),
]
# 보스는 국면을 색으로 구분하고 눈에 띄어야 해서, 일반 팔레트와 채도 당기기 대신 보스 그림에서 뽑은 팔레트를 쓴다(11/6)
EFFECT_STRAND = 40       # 그림 키 / 이 값보다 가는 가닥은 이펙트(300px 그림이면 7px)
EFFECT_PIECE = 0.15      # 가장 큰 몸통 덩어리의 이 비율보다 작은 덩어리는 이펙트
EFFECT_CHROMA = 0.13     # 이보다 채도가 높고
EFFECT_LIGHT = 0.66      # 이보다 밝으면 불꽃, 빛으로 본다


def core_mask(a):
    # 외곽선은 캐릭터 몸통에만 칠한다. 불씨, 번개, 빛까지 칠하면 이펙트가 검게 지저분해진다(11/6 사용자 지적)
    m = (a[..., 3] > 0.5).astype(np.uint8)
    k = max(3, int(round(a.shape[0] / EFFECT_STRAND)) | 1)
    op = cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(op, connectivity=8)
    if n <= 1:
        return m.astype(bool)
    big = st[1:, 4].max()
    keep = np.isin(lab, [i for i in range(1, n) if st[i, 4] >= big * EFFECT_PIECE])
    c = pp.oklab(a[..., :3].reshape(-1, 3)).reshape(a.shape[:2] + (3,))
    bright = (np.hypot(c[..., 1], c[..., 2]) > EFFECT_CHROMA) & (c[..., 0] > EFFECT_LIGHT)
    return keep & ~bright


def load_palette():
    colors = np.array(json.load(open(PALETTE))["colors"])
    return colors, pp.oklab(colors)


def build(rgba, ow, oh, palette, pixelize=True):
    core = np.asarray(Image.fromarray((core_mask(rgba) * 255).astype(np.uint8)).resize((ow, oh), Image.BOX)) / 255.0 > 0.5
    core = cv2.dilate(core.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    # 줄이지 않는 초상화는 PixelOE를 거치지 않는다. 외곽선 늘리기가 눈썹 같은 어두운 선을 두껍게 만든다(11/6 분노 표정)
    out = pp.remove_islands(pp.shrink(rgba, ow, oh) if pixelize else rgba.copy())
    out[pp.edge(out[..., 3] > 0.5) & core, :3] = pp.INK
    colors, lab_c = palette
    lab = pp.oklab(out[..., :3].reshape(-1, 3))
    out[..., :3] = colors[np.argmin(((lab[:, None] - lab_c[None]) ** 2).sum(-1), 1)].reshape(out.shape[:2] + (3,))
    return out


KING_REFERENCE = "king_smug"   # 이 표정의 옛 키에 맞춰 시트 전체 배율 하나를 정한다
# 서 있는 표정. 시트에서도 표정마다 키가 다르게 그려져(smug 316, speechless 352px) 왕관 꼭대기~발끝 키를 평균으로 맞춘다.
# 앉거나 누운 표정은 키를 비교할 수 없어 시트 배율 그대로 둔다(11/6 smug만 작고 speechless만 큼)
KING_STANDING = ("king_smug", "king_letsgo", "king_speechless", "king_embarrassed", "king_rage")


def crown_to_feet(c):
    # 그림 맨 위 금색 덩어리(왕관) 꼭대기부터 발끝까지. 왕관 위 불꽃, 땀방울 같은 이펙트를 키에서 뺀다
    lab = pp.oklab(c[..., :3].reshape(-1, 3)).reshape(c.shape[:2] + (3,))
    hue = np.degrees(np.arctan2(lab[..., 2], lab[..., 1]))
    gold = (c[..., 3] > 0.5) & (np.hypot(lab[..., 1], lab[..., 2]) > 0.06) & (hue > 60) & (hue < 110) & (lab[..., 0] > 0.45)
    n, _, st, _ = cv2.connectedComponentsWithStats(cv2.dilate(gold.astype(np.uint8), np.ones((5, 5), np.uint8)), 8)
    tops = [st[i, 1] for i in range(1, n) if st[i, 4] > 1500]
    return c.shape[0] - (min(tops) if tops else 0)


def king_sources(backup_of, density, for_world):
    # 새 왕 시트 칸을 옛 300px 칸(발밑, 가운데)에 끼운다. 배율은 시트 전체에 하나를 써서 머리 크기를 9장 모두 같게 한다.
    # 표정마다 옛 키에 맞추면 표정마다 배율이 달라져 왕 크기가 바뀌어 보였다(11/6 사용자 지적).
    # 월드용은 표정마다 칸의 PPU가 달라(옛 그림이 칸마다 다른 PPU) 그만큼 보정해 월드에서 같은 배율이 되게 한다
    sheet = np.asarray(Image.open(KING_SHEET).convert("RGBA")) / 255.0
    cells = {}
    for name, (y0, y1), (x0, x1) in KING_CELLS:
        c = sheet[y0:y1, x0:x1].copy()
        c[..., 3] = (c[..., 3] > 0.5).astype(float)
        ys, xs = np.nonzero(c[..., 3])
        cells[name] = c[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    ref_old = np.asarray(Image.open(backup_of(f"King/{KING_REFERENCE}.png")).convert("RGBA"))[..., 3] > 127
    ref_rows = np.nonzero(ref_old.any(1))[0]
    k = (ref_rows.max() - ref_rows.min() + 1) / cells[KING_REFERENCE].shape[0]   # 시트 px → 칸 px
    olds = {name: np.asarray(Image.open(backup_of(f"King/{name}.png")).convert("RGBA")) / 255.0 for name in cells}
    standing = {name: crown_to_feet(cells[name]) for name in KING_STANDING}
    mean_standing = sum(standing.values()) / len(standing)
    pose = {name: mean_standing / standing[name] if name in standing else 1.0 for name in cells}
    scale = {name: k * pose[name] * (density[name] / density[KING_REFERENCE] if for_world else 1.0) for name in cells}
    # 칸(300px)에 안 들어가는 표정이 있으면 그 표정만 줄이지 않고 9장 모두 같은 비율로 줄인다(한 장만 작아지지 않게)
    fit = 1.0
    for name, c in cells.items():
        ch, cw = olds[name].shape[:2]
        bottom = np.nonzero(olds[name][..., 3] > 0.5)[0].max()
        fit = min(fit, (cw - 4) / (c.shape[1] * scale[name]), (bottom + 1) / (c.shape[0] * scale[name]))
    out = {}
    for name, c in cells.items():
        old = olds[name]
        cy, cx = np.nonzero(old[..., 3] > 0.5)
        ch, cw = old.shape[:2]
        s = scale[name] * fit
        fw, fh = max(1, round(c.shape[1] * s)), max(1, round(c.shape[0] * s))
        fig = np.asarray(Image.fromarray((c * 255).astype(np.uint8), "RGBA").resize((fw, fh), Image.LANCZOS)) / 255.0
        canvas = np.zeros_like(old)
        bottom = min(cy.max(), ch - 1)
        top = max(0, bottom - fh + 1)
        left = int(np.clip(round((cx.min() + cx.max()) / 2 - (fw - 1) / 2), 0, cw - fw))
        # 발밑을 옛 자리에 붙이고, 칸 위로 넘치는 부분은 잘라 낸다
        canvas[top:bottom + 1, left:left + fw] = fig[fh - (bottom - top + 1):]
        canvas[..., 3] = (canvas[..., 3] > 0.5).astype(float)
        out[name] = canvas
    return out


def main():
    os.makedirs(pp.BACKUP, exist_ok=True)
    os.makedirs(PORTRAIT_DIR, exist_ok=True)
    palette = load_palette()
    items = json.load(open(LIST, encoding="utf-8"))

    def backup_of(rel):
        src = os.path.join(pp.BACKUP, rel.replace("/", "_"))
        if not os.path.exists(src):
            shutil.copy2(os.path.join(pp.ART, rel), src)
        return src

    for it in items:
        backup_of(it["path"])
    density = {os.path.splitext(os.path.basename(it["path"]))[0]: it["density"] for it in items}
    kings = king_sources(backup_of, density, for_world=True)
    king_portraits = king_sources(backup_of, density, for_world=False)
    results = []
    for it in items:
        rel = it["path"]
        name = os.path.splitext(os.path.basename(rel))[0]
        rgba = kings[name] if name in kings else np.asarray(Image.open(backup_of(rel)).convert("RGBA")) / 255.0
        h, w = rgba.shape[:2]
        f = pp.TARGET / it["density"]
        ow, oh = max(1, round(w * f)), max(1, round(h * f))
        if it.get("method") == "box":
            out = pp.shrink_box(rgba, ow, oh)
        else:
            out = build(rgba, ow, oh, palette)
        pp.save(os.path.join(pp.ART, rel), out)
        results.append({"path": rel, "w": w, "h": h, "ow": ow, "oh": oh})
        if name in kings:
            # 컷인: 캔버스 크기 그대로. 컷인 칸 450(기준 1080)이면 720p에서 그림 1px = 화면 1px
            pp.save(os.path.join(PORTRAIT_DIR, name + ".png"), build(king_portraits[name], w, h, palette, pixelize=False))
        print(f"{rel}\t{w}x{h}\t{ow}x{oh}")
    json.dump(results, open(LIST.replace(".json", "_result.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
