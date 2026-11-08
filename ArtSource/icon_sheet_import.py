# 생성기로 뽑은 아이콘 시트(초록 배경, 확대된 픽셀 아트)를 원래 픽셀 칸으로 되돌려 40x40 아이콘으로 자른다
# 사용: Tools/pixeloe-env/Scripts/python.exe -I ArtSource/icon_sheet_import.py [bombs|artifacts]  (없으면 둘 다)
# 11/8 무기 4 + 스킬 3 시트(위 4칸, 아래 3칸), 아티팩트 10 시트(5칸씩 두 줄). 빠른연사의 잔상 선(왼쪽 가로줄)은 사용자 요청으로 지운다.
import json
import os

import numpy as np
from PIL import Image
import cv2
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pixel_ppu32 as pp

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "_Project", "05. Art")
DST_DIR = os.path.join(ART, "UI", "IconsPixel")
RESULT = os.path.join(ROOT, "ArtSource", "icon_pixel_result.json")
# 시트의 점 크기를 그대로 둔다(아이콘끼리 크기 차이가 의미 있음). 큰폭발이 불꽃까지 37칸이라 40칸 바탕에 놓는다
SIZE = 40
# 웹p 압축 잡음으로 칸마다 색이 조금씩 달라 그림 하나에 수백 색이 나왔다(11/8 사용자 지적).
# 게임 공통 64색에 맞춰 보니 보라(블랙홀)가 파랑으로, 금색 시계와 불꽃이 갈색, 분홍으로 바랬다(공통 팔레트에 진한 보라, 금색이 없음).
# 그래서 시트마다 그 안의 그림들끼리 팔레트를 함께 뽑아 같은 색을 나눠 쓴다
SET_COLORS = 24
EDGE = 90           # 이웃 칸과 이만큼 색이 달라야 픽셀 경계로 본다(RGB 합)
# 시트마다: 원본 파일, 줄마다 칸 수, 떨어진 조각 지우기, (줄, 줄 안 순서, 파일 이름).
# 아티팩트 시트는 옆 칸 그림 끝이 칸 경계를 넘어와 조각으로 남아(양말 왼쪽) 몸통과 떨어진 조각을 지운다. 폭탄 시트는 다발폭탄의 작은 폭탄이 떨어져 있어 지우지 않는다. 이름은 지금 데이터가 가리키는 IconsPixel 이름 그대로
SHEETS = {
    "bombs": ("icons_bombs_skills_gen.webp", (4, 3), False, [
        (0, 0, "Icon_bomb_balance"), (0, 1, "Icon_bomb_rapid"), (0, 2, "Icon_bomb_huge"), (0, 3, "bomb_tripple"),
        (1, 0, "Icon_blackhole"), (1, 1, "다발폭탄"), (1, 2, "순간폭탄"),
    ]),
    "artifacts": ("icons_artifacts_gen.webp", (5, 5), True, [
        (0, 0, "Artifact_LeatherGloves"), (0, 1, "Artifact_OldSocks"), (0, 2, "Artifact_LeatherShoes"),
        (0, 3, "Artifact_OldCoin"), (0, 4, "Artifact_LeatherCloak"),
        (1, 0, "Artifact_Pendant"), (1, 1, "Artifact_OldRing"), (1, 2, "Artifact_RedRibbon"),
        (1, 3, "Artifact_LeatherBelt"), (1, 4, "Artifact_LeatherHelm"),
    ]),
}
# 몸통과 떨어진 조각을 지울 아이콘. 빠른연사의 잔상 선은 몸통 왼쪽에 따로 떠 있다
KEEP_MAIN_ONLY = {"Icon_bomb_rapid"}


def pixel_period(img, lo=6.0, hi=24.0):
    # 픽셀 경계(색이 크게 바뀌는 곳)가 같은 간격으로 늘어선다. 간격 후보마다 경계 위치의 위상이 얼마나 모이는지 보고 가장 잘 모이는 값을 고른다
    def axis(g, coord):
        ys, xs = np.nonzero(g > EDGE)
        pos = (xs if coord == 0 else ys) + 0.5
        w = g[ys, xs]
        best = (0, 0, 0)
        for p in np.arange(lo, hi, 0.005):
            z = (w * np.exp(2j * np.pi * pos / p)).sum() / w.sum()
            if abs(z) > best[0]:
                best = (abs(z), p, np.angle(z) / (2 * np.pi) * p)
        return best[1], best[2] % best[1]
    px, ox = axis(np.abs(np.diff(img, axis=1)).sum(-1), 0)
    py, oy = axis(np.abs(np.diff(img, axis=0)).sum(-1), 1)
    return px, py, ox, oy


def downsample(img, px, py, ox, oy):
    # 칸 가운데 7x7의 중앙값을 그 칸 색으로 쓴다(웹p 압축 번짐과 경계 섞임을 피한다)
    h, w = img.shape[:2]
    xs = np.arange(ox - px, w, px) + px / 2
    ys = np.arange(oy - py, h, py) + py / 2
    xs, ys = xs[(xs >= 0) & (xs < w)], ys[(ys >= 0) & (ys < h)]
    out = np.zeros((len(ys), len(xs), 3))
    for j, cy in enumerate(ys.astype(int)):
        for i, cx in enumerate(xs.astype(int)):
            out[j, i] = np.median(img[max(0, cy - 3):cy + 4, max(0, cx - 3):cx + 4].reshape(-1, 3), 0)
    return out, xs, ys


def to_palette(rgb, mask, palette_lab, palette_rgb):
    lab = pp.oklab(rgb[mask] / 255.0)
    idx = np.argmin(((lab[:, None] - palette_lab[None]) ** 2).sum(-1), 1)
    out = rgb.copy()
    out[mask] = palette_rgb[idx] * 255
    return out


def import_sheet(file, row_counts, drop_stray, icons):
    src = os.path.join(ROOT, "ArtSource", "Original", "UI", file)
    img = np.asarray(Image.open(src).convert("RGB")).astype(float)
    gp = pixel_period(img)
    print(f"sheet pixel {gp[0]:.3f} x {gp[1]:.3f}")
    results = json.load(open(RESULT, encoding="utf-8")) if os.path.exists(RESULT) else []
    by_name = {os.path.splitext(os.path.basename(r["dst"]))[0]: r for r in results}
    for _, _, name in icons:
        if name not in by_name:
            by_name[name] = {"dst": "UI/IconsPixel/" + name + ".png"}
            results.append(by_name[name])
    img_h, img_w = img.shape[:2]
    outs = []
    for row, col, name in icons:
        n = row_counts[row]
        x0, x1 = int(col * img_w / n), int((col + 1) * img_w / n)
        y0, y1 = int(row * img_h / 2), int((row + 1) * img_h / 2)
        part = img[y0:y1, x0:x1]
        # 시트 전체로 잰 칸 간격은 시트 끝으로 갈수록 반 칸 가까이 밀려 둥근 폭탄이 찌그러졌다(11/8). 그림마다 따로 잰다
        px, py, ox, oy = pixel_period(part, gp[0] - 0.6, gp[0] + 0.6)
        cell, _, _ = downsample(part, px, py, ox, oy)
        # 경계 칸은 초록 배경과 섞여 어두운 초록으로 남는다. 그림에 초록은 없으므로 초록 기운이 조금이라도 있으면 배경으로 본다
        mask = ~(cell[..., 1] > np.maximum(cell[..., 0], cell[..., 2]) + 25)
        if drop_stray or name in KEEP_MAIN_ONLY:
            count, labels = cv2.connectedComponents(mask.astype(np.uint8), connectivity=8)
            sizes = [(labels == k).sum() for k in range(1, count)]
            main = labels == (int(np.argmax(sizes)) + 1)
            # 불꽃처럼 몸통 바로 옆(대각 1칸)에 붙은 조각은 살리고, 1칸 이상 떨어진 잔상 선만 지운다
            near = cv2.dilate(main.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
            keep = np.zeros_like(mask)
            for k in range(1, count):
                piece = labels == k
                if (piece & near).any():
                    keep |= piece
            mask = keep
        if name in KEEP_MAIN_ONLY:
            # 몸통에 붙어 남은 잔상 선: 위아래가 비어 있는 1칸 두께 가로줄이 몸통 왼쪽 끝보다 왼쪽에 있으면 지운다
            keep = mask
            pad = np.pad(keep, 1)
            thin = keep & ~pad[:-2, 1:-1] & ~pad[2:, 1:-1]
            body_left = np.nonzero((keep & ~thin).any(0))[0].min()
            cols = np.arange(keep.shape[1])[None, :]
            keep &= ~(thin & (cols < body_left + 1))
            mask = keep
        ys_, xs_ = np.nonzero(mask)
        crop = cell[ys_.min():ys_.max() + 1, xs_.min():xs_.max() + 1]
        cm = mask[ys_.min():ys_.max() + 1, xs_.min():xs_.max() + 1]
        ch, cw = cm.shape
        if ch > SIZE or cw > SIZE:
            raise SystemExit(f"{name}: {cw}x{ch} 가 {SIZE}칸보다 큼")
        out = np.zeros((SIZE, SIZE, 4), np.uint8)
        oxp, oyp = (SIZE - cw) // 2, (SIZE - ch) // 2
        out[oyp:oyp + ch, oxp:oxp + cw, :3] = crop.round().clip(0, 255).astype(np.uint8)
        out[oyp:oyp + ch, oxp:oxp + cw, 3] = cm * 255
        outs.append((name, out))
        # 기준점은 그림 가운데. 폭탄 몸통 기준은 그림마다 손으로 볼 일이라 여기서는 맞추지 않는다
        by_name[name]["pivot"] = [0.5, 0.5]
        by_name[name]["src"] = "생성 시트 " + file
        print(f"{name}: {cw}x{ch}  pixel {px:.3f} x {py:.3f}")
    # 시트 안 그림들의 모든 칸에서 함께 팔레트를 뽑아 입힌다
    pixels = np.concatenate([o[o[..., 3] > 0][:, :3] for _, o in outs]) / 255.0
    palette_lab = pp.kmeans(pp.oklab(pixels), SET_COLORS)
    palette_rgb = pp.from_oklab(palette_lab)
    for name, out in outs:
        m = out[..., 3] > 0
        out[..., :3] = to_palette(out[..., :3].astype(float), m, palette_lab, palette_rgb).round().clip(0, 255).astype(np.uint8)
        Image.fromarray(out, "RGBA").save(os.path.join(DST_DIR, name + ".png"))
    json.dump(results, open(RESULT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def main():
    names = sys.argv[1:] or list(SHEETS)
    for key in names:
        import_sheet(*SHEETS[key])


if __name__ == "__main__":
    main()
