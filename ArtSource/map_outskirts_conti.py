# 맵 외곽(플레이어 이동 범위 밖, 카메라 범위 안)을 왕실 정원 구도로 다시 짠 콘티를 맵 촬영 그림 위에 그리는 스크립트
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/map_outskirts_conti.py
# 위 왕궁, 아래 정원이 이어짐, 오른쪽 정원사 구역, 왼쪽 수로. 가운데 놀이 공간은 그대로 둔다.
# 적 등장 구역 9개는 위치가 고정이라(게임 핵심), 구역마다 외곽을 지나 놀이 공간으로 들어오는 입구를 낸다.
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "Docs/map_conti/map_now.png"      # 카메라 범위 64×64유닛을 2048px로 찍은 그림
DST = ROOT / "Docs/map_conti/outskirts_conti_v2.png"
FONT = ROOT / "Assets/_Project/08. Fonts/NeoDunggeunmo/NeoDunggeunmoPro-Regular.ttf"
PPU = 32
CAM = (-32.02, 38.07, 31.98, -25.93)             # 카메라 범위(왼, 위, 오른, 아래)
PLAY = (-24.35, 23.69, 24.31, -17.77)            # 플레이어 이동 범위
# 씬의 [적]/스폰 구역 위치(유닛)와 크기. 입구 폭은 구역 크기보다 조금 넓게 낸다.
SPAWNS = {1: (-35.3, 27.2, 5.7), 2: (-35.5, 10.2, 4.4), 3: (-35.4, -10.2, 1.9), 4: (-12.3, -27.7, 8.0),
          6: (23.1, -28.0, 2.8), 7: (34.9, -13.2, 3.9), 8: (35.8, 13.8, 5.8), 9: (20.4, 30.3, 3.2), 10: (-15.6, 37.7, 3.1)}

STONE, ROOF = (226, 214, 190, 255), (86, 128, 196, 255)
WATER, BANK = (70, 120, 170, 255), (235, 228, 210, 255)
HEDGE, LEAF = (70, 130, 60, 255), (92, 150, 70, 255)
GROUND, PATH = (196, 182, 150, 235), (222, 206, 168, 255)
DARK = (40, 30, 20, 255)


def px(x: float, y: float) -> tuple:
    return int((x - CAM[0]) * PPU), int((CAM[1] - y) * PPU)


def box(d, x0, y0, x1, y1, fill, outline=DARK, width=3):
    a, b = px(x0, y0), px(x1, y1)
    d.rectangle([min(a[0], b[0]), min(a[1], b[1]), max(a[0], b[0]), max(a[1], b[1])], fill=fill, outline=outline, width=width)


def ellipse(d, x0, y0, x1, y1, fill, outline=DARK, width=3):
    a, b = px(x0, y0), px(x1, y1)
    d.ellipse([min(a[0], b[0]), min(a[1], b[1]), max(a[0], b[0]), max(a[1], b[1])], fill=fill, outline=outline, width=width)


def label(d, x, y, text, font, color=(255, 255, 255, 255)):
    d.multiline_text(px(x, y), text, font=font, fill=color, anchor="mm", align="center", stroke_width=4, stroke_fill=(30, 22, 15, 255), spacing=4)


def arrow(d, x0, y0, x1, y1, color=(235, 50, 50, 255)):
    # 등장 구역에서 놀이 공간까지 적이 들어오는 길
    a, b = px(x0, y0), px(x1, y1)
    d.line([a, b], fill=color, width=10)
    import math
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    for s in (2.6, -2.6):
        d.line([b, (b[0] - 34 * math.cos(ang + s / 6), b[1] - 34 * math.sin(ang + s / 6))], fill=color, width=10)


def main() -> None:
    base = Image.open(SRC).convert("RGBA")
    over = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(over)
    big = ImageFont.truetype(str(FONT), 34)
    small = ImageFont.truetype(str(FONT), 24)
    l, t, r, b = PLAY

    # 외곽은 지금 오브젝트를 모두 지운다고 보고 바닥색으로 덮는다
    box(d, CAM[0], CAM[1], CAM[2], t, GROUND, outline=None)
    box(d, CAM[0], b, CAM[2], CAM[3], GROUND, outline=None)
    box(d, CAM[0], t, l, b, GROUND, outline=None)
    box(d, r, t, CAM[2], b, GROUND, outline=None)

    # ── 위: 왕궁. 등장 10·9는 왕궁 날개 출입문에서 병사가 나온다(왕이 보낸 병사) ──
    box(d, -9, 38.07, 9, 28.5, STONE)                          # 본관
    box(d, -9, 38.07, 9, 34.5, ROOF)
    box(d, -2.5, 30.5, 2.5, 28.5, (150, 60, 50, 255))          # 정문(발코니 아래)
    label(d, 0, 33, "① 왕궁 본관\n발코니 + 정문", big)
    box(d, -26, 37, -9, 29.5, STONE)                           # 서쪽 날개
    box(d, -26, 37, -9, 34.5, ROOF)
    box(d, 9, 37, 26, 29.5, STONE)                             # 동쪽 날개
    box(d, 9, 37, 26, 34.5, ROOF)
    for x in (-15.6, 20.4):
        box(d, x - 2.2, 32.5, x + 2.2, 29.5, (110, 80, 60, 255))  # 병사 출입문(아치)
    label(d, -21.5, 32.3, "서쪽 날개", small)
    label(d, 14.5, 32.3, "동쪽 날개", small)
    label(d, -15.6, 36.0, "병사 출입문", small, (255, 220, 200, 255))
    label(d, 20.4, 36.0, "병사 출입문", small, (255, 220, 200, 255))
    ellipse(d, 26, 38.0, 31.8, 30.5, STONE)
    label(d, 28.9, 34.2, "탑", small)

    # 테라스, 난간. 계단은 가운데(왕) + 양쪽 출입문 앞(병사) 세 곳
    box(d, -26, 29.5, 26, 24.3, (214, 204, 184, 255))
    label(d, -5.8, 26.6, "② 테라스", small)
    box(d, l, 24.3, r, 23.69, (245, 240, 228, 255))            # 돌 난간 = 위쪽 경계
    box(d, -2.6, 26.0, 2.6, 23.4, (190, 180, 160, 255))        # 가운데 계단
    box(d, -0.9, 28.5, 0.9, 23.4, (178, 40, 40, 255), outline=None)  # 레드카펫
    label(d, 7.6, 26.6, "③ 왕의 계단 + 카펫", small)
    for x in (-15.6, 20.4):
        box(d, x - 2.4, 29.5, x + 2.4, 23.4, PATH)             # 출입문 → 옆 계단
    label(d, 9.5, 22.6, "돌 난간이 위쪽 경계, 계단 세 곳만 열림", small, (255, 240, 200, 255))

    # ── 왼쪽: 수로. 등장 1·2·3은 수로 건너편이라 다리 세 개 ──
    box(d, -31.6, 38.07, -25.6, CAM[3], WATER)
    box(d, -25.6, t, l, b, BANK)                               # 돌 둑 = 왼쪽 경계
    for n in (1, 2, 3):
        y = SPAWNS[n][1]
        w = max(SPAWNS[n][2], 3.0) / 2 + 0.6
        box(d, CAM[0], y + w, l, y - w, (175, 130, 90, 255))   # 다리
    label(d, -28.6, 18.5, "④ 직선\n수로", big)
    label(d, -28.6, 0.5, "돌 둑이\n왼쪽 경계", small, (255, 240, 200, 255))
    ellipse(d, -31.9, -17.5, -24.0, -25.8, WATER)              # 끝에서 넓어지는 연못
    ellipse(d, -30.0, -19.4, -25.9, -23.8, STONE)              # 물 위 정자
    label(d, -28, -21.6, "⑤ 물 위\n정자", small)

    # ── 오른쪽: 정원사 구역. 등장 8·7 자리는 과수 줄을 끊고 흙길을 낸다 ──
    gaps = [(SPAWNS[n][1], SPAWNS[n][2] / 2 + 0.8) for n in (8, 7)]
    for y in [23.0 - i * 2.6 for i in range(16)]:
        if any(abs(y - 1.0 - gy) < gw + 1.0 for gy, gw in gaps):
            continue
        ellipse(d, 24.4, y, 26.2, y - 1.9, LEAF)               # 과수 한 줄 = 오른쪽 경계
    for gy, gw in gaps:
        box(d, r, gy + gw, CAM[2], gy - gw, PATH)              # 흙길
    box(d, 26.8, 23.4, 31.8, 18.0, (190, 120, 80, 255))        # 정원사 오두막·창고
    label(d, 29.3, 20.7, "⑥ 오두막\n·창고", small)
    for i in range(4):
        y = 9.6 - i * 2.4
        box(d, 26.8, y, 31.6, y - 1.4, (120, 90, 60, 255), width=2)  # 텃밭 두둑
    label(d, 29.2, 5.5, "⑦ 텃밭", small)
    box(d, 26.6, -0.6, 31.8, -9.8, (200, 230, 230, 255))       # 온실
    for y in (-3.6, -6.6):
        d.line([px(26.6, y), px(31.8, y)], fill=(120, 160, 170, 255), width=2)
    label(d, 29.2, -5.2, "⑧ 온실", big)
    for x in (26.5, 29.0, 31.3):
        for y in (-19.0, -21.6, -24.2):
            ellipse(d, x - 0.7, y + 0.7, x + 0.7, y - 0.7, (230, 160, 60, 255))  # 오렌지 화분
    label(d, 29.0, -17.6, "⑨ 오렌지 화분", small)

    # ── 아래: 정원이 이어짐. 등장 4·6 자리는 생울타리에 토피어리 아치 문 ──
    box(d, l, b, r, b - 0.9, HEDGE)                            # 낮은 생울타리 = 아래쪽 경계
    for n in (4, 6):
        x, w = SPAWNS[n][0], SPAWNS[n][2] / 2 + 0.6
        box(d, x - w, b + 0.2, x + w, CAM[3], PATH)            # 자갈 길
        for sx in (x - w - 0.6, x + w + 0.6):
            ellipse(d, sx - 0.8, b + 0.6, sx + 0.8, b - 1.4, (50, 110, 45, 255))  # 아치 기둥 토피어리
    for x in (-21.5, -5.5, 5.5, 13.0):
        ellipse(d, x - 0.7, b + 0.4, x + 0.7, b - 1.3, (50, 110, 45, 255))       # 원뿔 토피어리
    label(d, 3.5, b + 1.2, "낮은 생울타리가 아래쪽 경계, 등장 4·6 자리에 아치 문", small, (255, 240, 200, 255))
    box(d, -23.6, -19.8, -18.2, -25.6, (170, 160, 130, 255))   # 자수 화단(왼쪽 작게)
    ellipse(d, -22.6, -20.8, -19.2, -24.6, (190, 60, 70, 255), outline=HEDGE, width=4)
    ellipse(d, -4.2, -19.6, 4.2, -25.8, (110, 160, 200, 255))  # 분수(중심축)
    label(d, 0, -22.7, "⑩ 분수", big)
    box(d, 5.5, -19.8, 19.6, -25.6, (170, 160, 130, 255))      # 자수 화단(오른쪽)
    ellipse(d, 7.8, -20.6, 17.3, -24.8, None, outline=HEDGE, width=6)
    ellipse(d, 10.3, -21.5, 14.8, -23.9, (190, 60, 70, 255), outline=HEDGE, width=4)
    label(d, 12.5, -18.9, "⑪ 자수 화단", small)

    # 중심축: 왕궁 정문 → 계단 → 놀이 공간 → 분수
    a, bb = px(0, 28.5), px(0, CAM[3])
    for y in range(a[1], bb[1], 40):
        d.line([(a[0], y), (a[0], min(y + 22, bb[1]))], fill=(255, 210, 60, 230), width=5)

    # 등장 구역과 들어오는 길
    ins = {1: (l + 1.5, 27.2 if 27.2 < t else t - 1.5), 2: (l + 1.5, 10.2), 3: (l + 1.5, -10.2), 4: (-12.3, b + 1.5),
           6: (23.1, b + 1.5), 7: (r - 1.5, -13.2), 8: (r - 1.5, 13.8), 9: (20.4, t - 1.5), 10: (-15.6, t - 1.5)}
    for n, (x, y, size) in SPAWNS.items():
        cx = min(max(x, CAM[0] + 1.2), CAM[2] - 1.2)
        cy = min(max(y, CAM[3] + 1.2), CAM[1] - 1.2)
        if n == 9:
            cy = 31.0
        if n == 10:
            cy = 31.0
        arrow(d, cx, cy, *ins[n])
        ellipse(d, cx - 1.2, cy + 1.2, cx + 1.2, cy - 1.2, (235, 50, 50, 255), outline=(255, 255, 255, 255), width=4)
        label(d, cx, cy, str(n), small)
    label(d, -9.0, -14.6, "빨간 원 = 적 등장 구역(번호는 씬 이름)", small, (255, 120, 110, 255))

    # 놀이 공간 테두리
    d.rectangle([px(l, t), px(r, b)], outline=(230, 40, 40, 255), width=6)
    label(d, -12.5, 21.6, "놀이 공간은 그대로", big, (255, 120, 110, 255))

    out = Image.alpha_composite(base, over).convert("RGB")
    out.save(DST)
    print(DST.name, out.size)


if __name__ == "__main__":
    main()
