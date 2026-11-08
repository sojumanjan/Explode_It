# 소품 그림체 참조 시트로 새로 뽑은 가벼운 건물(흰 배경)을 지금 맵 건물 자리와 넓이에 맞춰 PPU 32 점 밀도, 64색으로 덮어쓴다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/building_v4_import.py
# 결과(그림 크기, 기준점)는 building_v4_import_result.json에 쓰고, Unity에서 그 값으로 스프라이트 기준점을 다시 잡는다.
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
import prop_import as pi

SRC = os.path.join(pp.ROOT, "ArtSource", "Original", "buildings_v4")
BACKUP = os.path.join(pp.ROOT, "ArtSource", "Original", "buildings_v3_map")
RESULT = os.path.join(pp.ROOT, "ArtSource", "building_v4_import_result.json")

# 생성 그림 → 덮어쓸 맵 그림. 같은 파일에 덮어써서 씬 참조(GUID)를 그대로 둔다.
# 예전 건물(v3)은 그림 한 점을 2x2로 키워 점이 굵었다. 새 건물은 소품처럼 PPU 32에 한 점씩 그려 점 밀도를 소품과 맞춘다.
# 모두 Single, PPU 32, 배율 1, 기준점은 가로 가운데와 바닥에서 6px이다.
TARGETS = [
    ("선술집1.webp", "선술집1_hd.png"),
    ("선술집2.webp", "선술집2_hd.png"),
    ("선술집3.webp", "선술집3.png"),
    ("기사 훈련 건물.webp", "기사 훈련 건물 2_hd.png"),
    ("파란 원형 성.png", "파란 원형 성1_hd.png"),
    ("풍차.webp", "풍차1_hd.png"),
]
OLD_PIVOT_Y = 6
# 풍차 날개와 몸통 사이처럼 그림 안에 갇힌 흰 배경을 지울 건물
OPEN_HOLES = {"풍차1_hd.png"}

# 파란 원형 성 양쪽 벽: 검은 외곽선 바로 안쪽에 생성 그림의 갈색 테두리가 줄이면서 갈색, 잿빛, 황토색 점으로 섞여 남고,
# 가장자리가 몇 줄마다 1px씩 들쭉날쭉해서 울긋불긋해 보였다(11/9 사용자 지적).
# 위아래 3줄 다수결로 가장자리를 곧게 펴고, 외곽선 바로 안쪽의 짙은 점은 한 칸 더 안쪽 색으로 채운다.
EDGE_CLEAN = {"파란 원형 성1_hd.png"}
EDGE_DARK = 0.45     # 외곽선 안쪽 점이 이보다 어두우면 갈색 테두리 찌꺼기로 본다

# 기사 훈련 건물만 지붕에 짙은 파랑과 짙은 청록이 많아 다른 건물보다 무거워 보였다(11/9 사용자 지적).
# 처음엔 팔레트를 입힌 뒤 파란 색을 순위대로 통째로 바꿨더니 밝은 하늘색과 짙은 파랑이 얼룩덜룩 섞였다(11/9 사용자 지적).
# 팔레트 전 원본 그림에서 파란 칸의 Lab 평균과 퍼짐을 기준 건물(파란 원형 성) 원본의 파란 칸에 맞춘다. 그 뒤 팔레트를 입히면 원형 성처럼 매끄럽게 단계가 진다.
BLUE_MATCH = {"기사 훈련 건물 2_hd.png": "파란 원형 성.png"}


# 기사 훈련 건물 문으로 적이 나오게 해서(11/9 사용자 요청) 문짝과 그 테두리, 문 아래 디딤돌까지 투명하게 뚫는다.
# 문짝은 그림 아래 가운데에 있는 가장 큰 나무색(주황 갈색) 덩어리로 찾는다. 뚫은 둘레에는 아래 외곽선 단계에서 검은 줄이 생긴다.
DOOR_CUT = {"기사 훈련 건물 2_hd.png"}
DOOR_RIM = 1         # 문짝 둘레 짙은 테두리까지 함께 지울 폭(px)


def cut_door(rgba):
    rgb, mask = rgba[..., :3], rgba[..., 3] > 0.5
    h, w = mask.shape
    sat = pi.saturation(rgb)
    lum = pp.lum(rgb)
    wood = mask & (sat > 0.35) & (rgb[..., 0] > rgb[..., 1]) & (rgb[..., 1] > rgb[..., 2]) & (lum > 0.2) & (lum < 0.65)
    n, labels, stats, _ = cv2.connectedComponentsWithStats(wood.astype(np.uint8), connectivity=4)
    # 그림 아래쪽 절반, 가로 가운데 1/3 안에 중심이 있는 덩어리. 양쪽 문짝이 가운데 짙은 틈으로 갈려 두 덩어리라
    # 가장 큰 덩어리의 절반 넓이 이상인 것을 모두 문짝으로 본다.
    cands = []
    for i in range(1, n):
        x, y, bw, bh, area = stats[i]
        cx, cy = x + bw / 2, y + bh / 2
        if w / 3 < cx < w * 2 / 3 and cy > h / 2:
            cands.append((area, i))
    biggest = max(cands)[0]
    door = np.isin(labels, [i for area, i in cands if area >= biggest / 2])
    # 두 문짝 사이 틈도 문이다
    door = cv2.morphologyEx(door.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((1, 7), np.uint8)) > 0
    # 문고리 같은 작은 금색 점은 문짝 안에 갇힌 구멍이라 채운다
    inv = (~door).astype(np.uint8)
    n2, lab2 = cv2.connectedComponents(inv, connectivity=4)
    border = np.unique(np.concatenate([lab2[0], lab2[-1], lab2[:, 0], lab2[:, -1]]))
    door |= ~np.isin(lab2, border)
    door = cv2.dilate(door.astype(np.uint8), np.ones((3, 3), np.uint8), iterations=DOOR_RIM) > 0
    # 문 아래 디딤돌: 왼쪽 문짝 바닥이 오른쪽보다 높아 맨 아래 줄 폭만 보면 왼쪽 디딤돌 조각이 남았다.
    # 문 아래쪽 절반에 걸친 세로 줄마다, 그 줄의 문 바닥부터 그림 바닥까지 뚫는다.
    ys, xs = np.nonzero(door)
    lower = ys >= (ys.min() + ys.max()) // 2
    for x in range(xs[lower].min(), xs[lower].max() + 1):
        col = np.nonzero(door[:, x])[0]
        if len(col):
            door[col.max():, x] = True
    out = rgba.copy()
    out[door, 3] = 0
    # 문 둘레를 지우고 남은 부스러기(뚫린 자리 안의 점, 가는 세로 줄)를 없앤다
    n, labels, stats, _ = cv2.connectedComponentsWithStats((out[..., 3] > 0.5).astype(np.uint8), connectivity=4)
    small = np.zeros(n, bool)
    small[1:] = stats[1:, cv2.CC_STAT_AREA] < 20
    out[small[labels], 3] = 0
    return out


def straighten_edges(rgba):
    mask = rgba[..., 3] > 0.5
    pad = np.pad(mask, ((1, 1), (0, 0)))
    vote = pad[:-2].astype(int) + pad[1:-1] + pad[2:]
    new = vote >= 2
    out = rgba.copy()
    out[~new, 3] = 0
    # 새로 채운 칸은 위나 아래 칸 색을 쓴다
    for y, x in zip(*np.nonzero(new & ~mask)):
        src = y - 1 if y > 0 and mask[y - 1, x] else y + 1
        out[y, x] = rgba[src, x]
    return out


def clean_inner_rim(rgba):
    # 검은 외곽선(바깥과 맞닿은 칸) 바로 안쪽 칸 중 짙은 점을, 외곽선 반대쪽으로 한 칸 더 들어간 칸 색으로 바꾼다
    mask = rgba[..., 3] > 0.5
    h, w = mask.shape
    pad = np.pad(mask, 1)
    edge = mask & ~(pad[2:, 1:-1] & pad[:-2, 1:-1] & pad[1:-1, 2:] & pad[1:-1, :-2])
    inner = mask & ~edge
    epad = np.pad(edge, 1)
    rim = inner & (epad[2:, 1:-1] | epad[:-2, 1:-1] | epad[1:-1, 2:] | epad[1:-1, :-2])
    out = rgba.copy()
    for y, x in zip(*np.nonzero(rim & (pp.lum(rgba[..., :3]) < EDGE_DARK))):
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            ey, ex = y - dy, x - dx
            iy, ix = y + dy, x + dx
            if 0 <= ey < h and 0 <= ex < w and edge[ey, ex] and 0 <= iy < h and 0 <= ix < w and inner[iy, ix] and not rim[iy, ix]:
                out[y, x] = rgba[iy, ix]
                break
    return out


def blue_mask(rgba):
    rgb = rgba[..., :3]
    return (rgba[..., 3] > 0.5) & (rgb[..., 2] > rgb[..., 0] + 0.15) & (rgb[..., 2] >= rgb[..., 1])


def match_blues(rgba, ref_rgba):
    lab = cv2.cvtColor(rgba[..., :3].astype(np.float32), cv2.COLOR_RGB2Lab)
    ref = cv2.cvtColor(ref_rgba[..., :3].astype(np.float32), cv2.COLOR_RGB2Lab)
    m, rm = blue_mask(rgba), blue_mask(ref_rgba)
    src, tgt = lab[m], ref[rm]
    lab[m] = (src - src.mean(0)) / np.maximum(src.std(0), 1e-6) * tgt.std(0) + tgt.mean(0)
    out = rgba.copy()
    out[..., :3] = np.clip(cv2.cvtColor(lab, cv2.COLOR_Lab2RGB), 0, 1)
    return out


def main():
    os.makedirs(BACKUP, exist_ok=True)
    palette = pp.load_palette()
    result = {}
    for src_name, dst_name in TARGETS:
        dst = os.path.join(bi.PROPS, dst_name)
        backup = os.path.join(BACKUP, dst_name)
        if not os.path.exists(backup):
            shutil.copy2(dst, backup)
        old = np.asarray(Image.open(backup).convert("RGBA"))[..., 3] > 127
        ys, xs = np.nonzero(old)
        bw, bh = xs.max() + 1 - xs.min(), ys.max() + 1 - ys.min()
        old_bottom = old.shape[0] - 1 - ys.max()
        rgba = pi.load_white_keyed(os.path.join(SRC, src_name), None, dst_name in OPEN_HOLES)
        if dst_name in BLUE_MATCH:
            rgba = match_blues(rgba, pi.load_white_keyed(os.path.join(SRC, BLUE_MATCH[dst_name]), None, False))
        # 새 그림은 비율이 예전과 달라 늘리지 않고, 맵에서 차지하는 넓이가 예전과 같도록 비율을 지켜 줄인다
        k = np.sqrt(bw * bh / (rgba.shape[1] * rgba.shape[0]))
        ow, oh = round(rgba.shape[1] * k), round(rgba.shape[0] * k)
        out = pp.shrink(rgba, ow, oh)
        out = pp.apply_palette(out, palette)
        out = pp.remove_islands(out)
        if dst_name in EDGE_CLEAN:
            out = straighten_edges(out)
        if dst_name in DOOR_CUT:
            out = cut_door(out)
        # 소품과 같이 비슷한 색을 덩어리로 모아 잘게 부서진 느낌을 줄인다(11/9 사용자 요청)
        out = pi.merge_similar(out)
        # 소품과 같은 검은 1px 외곽선. deep은 작은 소품에서 안쪽 그늘을 밝은 줄로 바꿔서 끈 것과 같이 끈다.
        out = pp.outline(out, "ink", deep=False)
        if dst_name in EDGE_CLEAN:
            out = clean_inner_rim(out)
        Image.fromarray((out * 255).round().astype(np.uint8), "RGBA").save(dst)
        # 기준점: 가로 가운데, 바닥에서 예전과 같은 비율 높이(예전 그림 바닥 위 6px)
        py = max(0.0, (OLD_PIVOT_Y - old_bottom) / bh * oh)
        result[dst_name] = {"size": [ow, oh], "pivot": [round(ow / 2, 2), round(py, 2)]}
        print(dst_name, (ow, oh), "old box", (int(bw), int(bh)), "pivot", result[dst_name]["pivot"])
    with open(RESULT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
