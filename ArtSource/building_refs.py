# 건물 아트를 다시 뽑을 때 생성기에 넣을 참조 이미지 3장(지금 디자인, 맞출 화풍, 팔레트)을 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/building_refs.py
import os

from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
PROPS = os.path.join(ROOT, "Assets", "_Project", "05. Art", "Props")
ART = os.path.join(ROOT, "Assets", "_Project", "05. Art")
OUT = os.path.join(ROOT, "Docs", "sprite_refs")
BG = (238, 230, 214, 255)

# 같은 PPU(32)라 픽셀 크기를 그대로 두고 나란히 놓으면 건물끼리 크기 비율도 함께 전달된다.
BUILDINGS = [
    "선술집1_hd.png",
    "선술집2_hd.png",
    "선술집3.png",
    "기사 훈련 건물 2_hd.png",
    "파란 원형 성1_hd.png",
    "풍차1_hd.png",
    "왕실 정자1.png",
]
# 정자는 PPU가 45라 픽셀이 다른 건물보다 작다. 씬에서 보이는 크기로 맞춰 넣어야 크기 비율이 맞는다.
PPU = {"왕실 정자1.png": 45.3}
ENTRY = "기사 훈련 건물 2_입구.png"
# 입구는 건물 그림 위에 따로 얹힌 조각이라, 씬과 같은 자리에 붙여 한 장으로 보여 준다(건물 피벗 기준 유닛 좌표).
ENTRY_OFFSET = (16.53 - 18.40, 24.59 - 24.24)
SCALE = 2
GAP = 24


def building_sheet():
    images = []
    for name in BUILDINGS:
        im = Image.open(os.path.join(PROPS, name)).convert("RGBA")
        if name.startswith("기사 훈련"):
            im = with_entry(im)
        if name in PPU:
            k = 32 / PPU[name]
            im = im.resize((round(im.width * k), round(im.height * k)), Image.NEAREST)
        images.append(im)
    width = sum(im.width for im in images) + GAP * (len(images) + 1)
    height = max(im.height for im in images) + GAP * 2
    sheet = Image.new("RGBA", (width, height), BG)
    x = GAP
    for im in images:
        sheet.alpha_composite(im, (x, height - GAP - im.height))
        x += im.width + GAP
    sheet = sheet.resize((width * SCALE, height * SCALE), Image.NEAREST)
    sheet.convert("RGB").save(os.path.join(OUT, "building_ref1_designs.png"))
    # 한 장에 7개를 뽑으면 건물마다 픽셀이 너무 적어 해상도가 깨져서(사용자 지적), 건물 하나씩 뽑을 디자인 참조도 따로 만든다.
    for i, im in enumerate(images, 1):
        single = Image.new("RGBA", (im.width + GAP * 2, im.height + GAP * 2), BG)
        single.alpha_composite(im, (GAP, GAP))
        k = max(1, 1024 // max(single.size))
        single = single.resize((single.width * k, single.height * k), Image.NEAREST)
        single.convert("RGB").save(os.path.join(OUT, "building_single_%d.png" % i))


def with_entry(building):
    entry = Image.open(os.path.join(PROPS, ENTRY)).convert("RGBA")
    # 건물 피벗(219, 56 아래에서)과 입구 피벗(가운데)을 맞춰 붙인다
    pivot_x, pivot_y = 219, building.height - 56
    cx = pivot_x + int(round(ENTRY_OFFSET[0] * 32))
    cy = pivot_y - int(round(ENTRY_OFFSET[1] * 32))
    out = building.copy()
    out.alpha_composite(entry, (cx - entry.width // 2, cy - entry.height // 2))
    return out


def style_sheet():
    # 맞출 화풍: 캐릭터(고블린, 기사, 방패병)만 같은 픽셀 크기로 놓는다.
    # 건물을 화풍 참조로 넣으면 그 건물의 잔 디테일까지 따라 그려서, 건물을 더 단순하게 하려는 목적(사용자 요청)과 어긋난다.
    names = [
        os.path.join(ART, "Player3.png"),
        os.path.join(ART, "Knight_hd.png"),
        os.path.join(ART, "Shielder_hd.png"),
    ]
    images = [Image.open(p).convert("RGBA") for p in names]
    width = sum(im.width for im in images) + GAP * (len(images) + 1)
    height = max(im.height for im in images) + GAP * 2
    sheet = Image.new("RGBA", (width, height), BG)
    x = GAP
    for im in images:
        sheet.alpha_composite(im, (x, height - GAP - im.height))
        x += im.width + GAP
    sheet = sheet.resize((width * SCALE, height * SCALE), Image.NEAREST)
    sheet.convert("RGB").save(os.path.join(OUT, "building_ref2_style.png"))


def palette_sheet():
    # 맵 팔레트(ArtSource/palette.png, 64색 한 줄)를 8x8 칸으로 다시 놓아 생성기가 색을 읽기 쉽게 한다
    strip = Image.open(os.path.join(ROOT, "ArtSource", "palette.png")).convert("RGB")
    count = strip.width // strip.height
    cell = 64
    cols = 8
    rows = (count + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell, rows * cell), BG[:3])
    draw = ImageDraw.Draw(sheet)
    for i in range(count):
        color = strip.getpixel((i * strip.height + strip.height // 2, strip.height // 2))
        x, y = (i % cols) * cell, (i // cols) * cell
        draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=color)
    sheet.save(os.path.join(OUT, "building_ref3_palette.png"))


def density_sheet():
    # PPU 32로 넣을 때 맞출 점 밀도: 맵에 이미 PPU 32로 들어간 소품과 고블린을 실제 점 크기 그대로(3배 확대만) 나란히 놓는다.
    # 3배로 키운 건물은 점이 너무 굵어(사용자 지적) 이 밀도로 다시 뽑는다.
    names = ["분수대1_hd.png", "마차_hd.png", "기사 석상1.png", "풀 울타리1.png", "꽃 가로등1.png"]
    images = [Image.open(os.path.join(PROPS, n)).convert("RGBA") for n in names]
    images.append(Image.open(os.path.join(ART, "Player3.png")).convert("RGBA"))
    gap = 12
    width = sum(im.width for im in images) + gap * (len(images) + 1)
    height = max(im.height for im in images) + gap * 2
    sheet = Image.new("RGBA", (width, height), BG)
    x = gap
    for im in images:
        sheet.alpha_composite(im, (x, height - gap - im.height))
        x += im.width + gap
    sheet = sheet.resize((width * 3, height * 3), Image.NEAREST)
    sheet.convert("RGB").save(os.path.join(OUT, "building_ref2_density.png"))


HEDGES = ["풀 울타리1.png", "풀 울타리3.png", "풀 울타리8.png", "풀 아치1.png", "풀 울타리 9.png"]
NEW_BUILDINGS = ["선술집2_hd.png", "선술집3.png", "기사 훈련 건물 2_hd.png", "풍차1_hd.png"]


def trimmed(path):
    im = Image.open(path).convert("RGBA")
    return im.crop(im.getbbox())


def row_sheet(images, scale, gap, name):
    width = sum(im.width for im in images) + gap * (len(images) + 1)
    height = max(im.height for im in images) + gap * 2
    sheet = Image.new("RGBA", (width, height), BG)
    x = gap
    for im in images:
        sheet.alpha_composite(im, (x, height - gap - im.height))
        x += im.width + gap
    sheet = sheet.resize((width * scale, height * scale), Image.NEAREST)
    sheet.convert("RGB").save(os.path.join(OUT, name))


def hedge_refs():
    # 풀 울타리를 새 건물 그림체로 다시 뽑을 참조: 지금 울타리 모양(투명 여백을 잘라 4배)과 맵에 들어간 새 건물(PPU 32 그대로 2배)
    row_sheet([trimmed(os.path.join(PROPS, n)) for n in HEDGES], 4, 8, "hedge_ref1_designs.png")
    row_sheet([trimmed(os.path.join(PROPS, n)) for n in NEW_BUILDINGS], 2, 16, "hedge_ref2_style.png")


if __name__ == "__main__":
    building_sheet()
    style_sheet()
    palette_sheet()
    density_sheet()
    hedge_refs()
    print("ok")
