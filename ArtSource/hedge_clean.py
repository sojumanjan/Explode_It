# 풀 울타리, 풀 아치 그림의 초록을 5단계로 줄이고 튀는 점을 다수결로 정리해 잎이 덜 자글거리게 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/hedge_clean.py
# 원본(정리 전)은 ArtSource/Original/hedges_v1에 한 번만 복사해 두고, 다시 돌리면 늘 원본에서 시작한다.
import os
import shutil

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROPS = os.path.join(ROOT, "Assets", "_Project", "05. Art", "Props")
BACKUP = os.path.join(ROOT, "ArtSource", "Original", "hedges_v1")

FILES = [
    "풀 울타리1.png", "풀 울타리3.png", "풀 울타리4.png", "풀 울타리5.png",
    "풀 울타리6.png", "풀 울타리7.png", "풀 울타리8.png", "풀 아치1.png",
    "풀 울타리 9.png",
]

# 64색 팔레트의 초록 12단계(어두운 → 밝은)를 5단계로 묶는다. 묶은 색도 모두 팔레트 색이라 팔레트를 벗어나지 않는다.
# 잎 한 장 한 장이 1~2px 밝고 어두운 점으로 12단계에 흩어져 맵 전체가 살아 움직이듯 자글거렸다(11/8 사용자 지적).
GREEN_GROUPS = [
    ((20, 44, 16), [(12, 29, 10), (20, 44, 16)]),
    ((38, 70, 30), [(29, 57, 23), (38, 70, 30), (50, 85, 38)]),
    ((62, 102, 47), [(62, 102, 47), (77, 118, 55)]),
    ((91, 133, 64), [(91, 133, 64), (106, 149, 68)]),
    ((124, 167, 77), [(124, 167, 77), (146, 189, 84), (183, 214, 88)]),
]
CLEAN_PASSES = 2


def labels_of(rgb, alpha):
    # 초록 칸은 단계 번호(0~4), 꽃, 외곽선, 나무 등 다른 색은 -2(그대로 둠), 투명은 -1
    label = np.full(alpha.shape, -2, int)
    for level, (_, members) in enumerate(GREEN_GROUPS):
        for c in members:
            label[np.all(rgb == c, -1)] = level
    label[~alpha] = -1
    return label


def clean(label):
    # 둘레 3x3 9칸 중 5칸 이상이 같은 초록 단계면 그 단계로 맞춰 잎이 덩어리로 뭉치게 한다. 꽃, 외곽선은 바꾸지도 세지도 않는다.
    # 책 정리처럼 가로, 세로 줄로 다수결을 내면 풀에는 붓으로 그은 듯한 세로, 가로 줄무늬가 생겼다.
    h, w = label.shape
    pad = np.pad(label, 1, mode="edge")
    out = label.copy()
    for y in range(h):
        for x in range(w):
            if label[y, x] < 0:
                continue
            block = pad[y:y + 3, x:x + 3].ravel()
            greens = block[block >= 0]
            vals, counts = np.unique(greens, return_counts=True)
            i = counts.argmax()
            if counts[i] >= 5:
                out[y, x] = vals[i]
    return out


def process(name):
    src = os.path.join(BACKUP, name)
    dst = os.path.join(PROPS, name)
    if not os.path.exists(src):
        shutil.copy2(dst, src)
    rgba = np.asarray(Image.open(src).convert("RGBA")).copy()
    alpha = rgba[..., 3] > 127
    label = labels_of(rgba[..., :3].astype(int), alpha)
    for _ in range(CLEAN_PASSES):
        label = clean(label)
    for level, (color, _) in enumerate(GREEN_GROUPS):
        rgba[label == level, :3] = color
    Image.fromarray(rgba, "RGBA").save(dst)
    before = len(np.unique(np.asarray(Image.open(src).convert("RGB"))[alpha], axis=0))
    after = len(np.unique(rgba[..., :3][alpha], axis=0))
    print(f"{name}: {before} -> {after} colors")


if __name__ == "__main__":
    os.makedirs(BACKUP, exist_ok=True)
    for f in FILES:
        process(f)
