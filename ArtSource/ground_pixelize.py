# 생성한 흙 바닥 그림을 가짜 픽셀 격자에 맞춰 줄이고, 세로 이음매를 이어 붙여 반복 타일로 만드는 스크립트
# 사용: py -3.13 ArtSource/ground_pixelize.py [출력 폴더]
# 바닥 타일(네모 돌)은 비율을 눕혀도 위에서 본 무늬로 읽혀 캐릭터와 각도가 따로 놀았다(11/18). 방향 없는 흙 바닥으로 바꾼다.
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "ArtSource/Original/ground"
# 그림마다 원본의 가짜 점 크기(px). 가장자리 세기의 주기로 쟀다.
CELL = {"ground_a.webp": 8.0, "ground_b.webp": 1024 / 200, "ground_c.webp": 1024 / 200}
COLORS = 8          # 흙은 2~3톤 얼룩과 작은 자갈뿐이라 적게 둔다
SEAM_BAND = 128     # 세로 이음매를 섞는 띠 높이(원본 px). 생성 그림은 위아래가 맞지 않았다(가로는 거의 맞음)


def blend_vertical_seam(a: np.ndarray, band: int) -> np.ndarray:
    # 아래 띠를 위 띠에 점점 섞어 넣고 아래 띠를 잘라 낸다. 새 마지막 줄 다음이 원래 아래 띠 첫 줄이 되어 이어진다.
    h = a.shape[0]
    w = (np.arange(band) / band)[:, None, None]
    top = (1 - w) * a[h - band:] + w * a[:band]
    return np.concatenate([top, a[band:h - band]], axis=0)


def grid_phase(lum: np.ndarray, cell: float, axis: int) -> int:
    # 가짜 점 경계가 출력 칸 경계와 맞도록 어긋난 만큼(원본 px)을 찾는다.
    d = np.abs(np.diff(lum, axis=axis)).sum(axis=1 - axis)
    idx = np.arange(len(d)) + 1
    scores = [d[np.abs((idx - o) / cell - np.round((idx - o) / cell)) < 0.5 / cell].sum()
              for o in range(int(round(cell)))]
    return int(np.argmax(scores))


def pixelize(path: Path) -> Image.Image:
    cell = CELL[path.name]
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    lum = a @ np.array([0.299, 0.587, 0.114])
    # 격자를 0에서 시작하게 민다(가로는 이어지므로 말아 돌려도 된다. 세로는 이음매 처리 전에 민다).
    a = np.roll(a, -grid_phase(lum, cell, 1), axis=1)
    a = np.roll(a, -grid_phase(lum, cell, 0), axis=0)
    a = blend_vertical_seam(a, SEAM_BAND)
    h, w = a.shape[:2]
    ow, oh = int(round(w / cell)), int(round(h / cell))
    # 원본 해상도에서 색을 먼저 나눈 뒤 칸마다 가장 많은 색 하나만 남긴다(평균을 내면 중간 색이 생긴다).
    q = Image.fromarray(a.astype(np.uint8)).quantize(COLORS, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    lab = np.asarray(q)
    pal = np.array(q.getpalette()[:COLORS * 3]).reshape(-1, 3)
    out = np.zeros((oh, ow), np.int32)
    ys = np.linspace(0, h, oh + 1).astype(int)
    xs = np.linspace(0, w, ow + 1).astype(int)
    for j in range(oh):
        for i in range(ow):
            c = lab[ys[j]:ys[j + 1], xs[i]:xs[i + 1]]
            out[j, i] = np.bincount(c.ravel(), minlength=COLORS).argmax()
    return Image.fromarray(pal[out].astype(np.uint8))


def main() -> None:
    out_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else SRC
    for p in sorted(SRC.glob("ground_*.webp")):
        img = pixelize(p)
        img.save(out_dir / (p.stem + "_px.png"))
        print(p.name, img.size)


if __name__ == "__main__":
    main()
