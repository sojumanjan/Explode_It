# 그림체가 안 맞는 맵 소품을 다시 뽑을 때 생성기에 넣을 참조(맞출 화풍 한 장, 소품마다 지금 모양 한 장)를 만든다
# 사용: Tools/pixeloe-env/Scripts/python.exe ArtSource/prop_refs.py
import os

from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
PROPS = os.path.join(ROOT, "Assets", "_Project", "05. Art", "Props")
OUT = os.path.join(ROOT, "Docs", "sprite_refs")
BG = (238, 230, 214, 255)
GAP = 16

# 맞출 화풍: 새 그림체로 들어간 건물과 사각 울타리 벽. 같은 PPU 32라 크기 그대로 놓으면 점 밀도도 함께 전달된다.
STYLE = ["선술집2_hd.png", "선술집3.png", "풍차1_hd.png", "풀 울타리 벽 외곽.png"]
# 다시 뽑을 소품. 마차2와 무기 거치대2는 1을 좌우로 뒤집은 그림이라 따로 참조를 만들지 않는다.
PROP_NAMES = {
    "마차_hd.png": "prop_single_wagon.png",
    "분수대1_hd.png": "prop_single_fountain.png",
    "나무1.png": "prop_single_tree.png",
    "테이블 의자세트1.png": "prop_single_table.png",
    "보급 상자1.png": "prop_single_supply.png",
    "무기 거치대1.png": "prop_single_weapon_rack.png",
    "선술집 조형물1.png": "prop_single_tavern_display.png",
    "나무 상자1.png": "prop_single_chest.png",
    "나무 벤치1.png": "prop_single_bench.png",
    "3단 오크통1.png": "prop_single_barrels.png",
}


def trimmed(name):
    im = Image.open(os.path.join(PROPS, name)).convert("RGBA")
    return im.crop(im.getbbox())


def style_sheet():
    images = [trimmed(n) for n in STYLE]
    width = sum(im.width for im in images) + GAP * (len(images) + 1)
    height = max(im.height for im in images) + GAP * 2
    sheet = Image.new("RGBA", (width, height), BG)
    x = GAP
    for im in images:
        sheet.alpha_composite(im, (x, height - GAP - im.height))
        x += im.width + GAP
    sheet.resize((width * 2, height * 2), Image.NEAREST).convert("RGB").save(os.path.join(OUT, "prop_ref_style.png"))


def single_sheets():
    # 작은 소품은 점이 몇 개 안 돼 생성기가 모양을 못 읽어서, 1024px 안팎이 되도록 정수배로 키운다
    for name, out in PROP_NAMES.items():
        im = trimmed(name)
        single = Image.new("RGBA", (im.width + GAP * 2, im.height + GAP * 2), BG)
        single.alpha_composite(im, (GAP, GAP))
        k = max(1, 1024 // max(single.size))
        single.resize((single.width * k, single.height * k), Image.NEAREST).convert("RGB").save(os.path.join(OUT, out))


# 건물을 다시 뽑을 때 맞출 화풍(11/9): 새로 넣은 울타리, 아치, 정자, 마차 그림체 소품. 건물 형태는 참조하지 않는다.
# 모두 맵에서 1유닛에 약 32점이라 같은 배율로 키워 점 밀도와 크기 비례를 함께 보여 준다.
NEW_STYLE_ROWS = [
    ["풀 울타리 벽 외곽.png", "풀 아치1.png", "왕실 정자1.png", "마차_hd.png", "분수대1_hd.png", "나무1.png"],
    ["테이블 의자세트1.png", "보급 상자1.png", "무기 거치대1.png", "선술집 조형물1.png", "나무 상자1.png", "나무 벤치1.png", "3단 오크통1.png"],
]
NEW_STYLE_SCALE = 3

# 쇠 울타리를 다시 뽑을 때 맞출 화풍(11/9): 새로 넣은 건물, 사각 풀 울타리, 아치, 정자, 마차
FENCE_STYLE_ROWS = [
    ["선술집1_hd.png", "선술집2_hd.png", "선술집3.png", "기사 훈련 건물 2_hd.png", "파란 원형 성1_hd.png", "풍차1_hd.png"],
    ["풀 울타리 벽 외곽.png", "풀 아치1.png", "왕실 정자1.png", "마차_hd.png"],
]
FENCE_STYLE_SCALE = 2


def building_style_sheet(row_names=NEW_STYLE_ROWS, out_name="building_ref_new_style.png", k=NEW_STYLE_SCALE):
    rows = [[trimmed(n) for n in names] for names in row_names]
    width = max(sum(im.width for im in row) + GAP * (len(row) + 1) for row in rows)
    heights = [max(im.height for im in row) + GAP for row in rows]
    sheet = Image.new("RGBA", (width, sum(heights) + GAP), BG)
    y = GAP
    for row, h in zip(rows, heights):
        x = GAP
        for im in row:
            sheet.alpha_composite(im, (x, y + h - GAP - im.height))
            x += im.width + GAP
        y += h
    sheet = sheet.resize((sheet.width * k, sheet.height * k), Image.NEAREST).convert("RGB")
    # 맵 64색 팔레트(ArtSource/palette.png 한 줄)를 아랫줄 오른쪽 빈자리에 8x8 칸으로 넣어 생성기가 쓸 색을 한 장에서 읽게 한다
    strip = Image.open(os.path.join(ROOT, "ArtSource", "palette.png")).convert("RGB")
    count = strip.width // strip.height
    cols = 8
    rows_n = (count + cols - 1) // cols
    cell = (heights[1] - GAP) * k // rows_n
    used = (sum(im.width for im in rows[1]) + GAP * len(rows[1])) * k
    x0 = used + (sheet.width - used - cols * cell) // 2
    y0 = (GAP + heights[0]) * k
    draw = ImageDraw.Draw(sheet)
    for i in range(count):
        color = strip.getpixel((i * strip.height + strip.height // 2, strip.height // 2))
        x, y = x0 + (i % cols) * cell, y0 + (i // cols) * cell
        draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=color)
    sheet.save(os.path.join(OUT, out_name))


if __name__ == "__main__":
    style_sheet()
    single_sheets()
    building_style_sheet()
    building_style_sheet(FENCE_STYLE_ROWS, "fence_ref_style.png", FENCE_STYLE_SCALE)
    print("ok")
