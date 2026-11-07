# 아트 기준서 (스프라이트 다시 뽑기용)

2026-11-06 작성. 게임 스프라이트를 이미지 생성 모델로 다시 뽑을 때 지키는 기준이다. 목적은 하나, **모든 그림이 한 사람이 한 번에 그린 것처럼 보이게** 하는 것이다. 지금 AI 티의 원인(시점, 팔레트, 빛, 비율이 그림마다 다름)을 기준으로 막는다.

---

## 1. 고정 규칙

| 항목 | 기준 |
|---|---|
| 시점 | 소품, 건물: 위에서 비스듬히 내려다보는 **3/4 탑다운**(바닥이 보이는 각도 약 45도), 정면 그림, 옆모습 그림 금지. 캐릭터: 서 있는 정면에 가까운 모습(11/6 시범 시트 v1 기준, 지금 게임 그림과도 맞음) |
| 빛 | **왼쪽 위**에서. 밝은 면은 왼쪽 위, 그림자는 오른쪽 아래 |
| 외곽선 | 바깥 실루엣에 **검정 1px**(최종 32px 기준). 안쪽 선은 같은 색 계열의 어두운 색 |
| 팔레트 | `ArtSource/palette.png` 64색. 생성할 때 참조로 넣고, 받은 뒤 변환 스크립트가 이 팔레트로 다시 칠한다 |
| 밀도 | 최종 **1유닛 = 32px**. 생성은 최종의 **4배**(1유닛 = 128px)로 받아 줄인다 |
| 그림체 | 큰 색 덩어리, 단순한 형태, 잔무늬 없음. 질감은 2~3단계 음영으로만. 그러데이션, 블러, 빛 번짐 금지 |
| 배경 | **단색 마젠타 `#FF00FF`**. 투명 배경을 시키면 바둑판 무늬를 그려 넣는다(핸드오프 2번) |
| 바닥 그림자 | 그리지 않는다. 게임이 따로 그린다 |
| 자세 | 캐릭터는 제자리 전투 자세 한 장. 걷기, 공격은 코드가 출렁임, 눌림으로 만든다 |
| 방향 | 플레이어(`PlayerVisual`)와 적(`EnemyVisual`) 모두 프리팹의 `_artFacesRight`(그림이 원래 보는 방향)를 기준으로 진행 방향에 맞춰 좌우를 뒤집는다. 새 그림 방향이 옛 그림과 다르면 값을 바꾸지 말고 `apply_sheet.py`의 좌우 뒤집기 칸으로 그림을 옛 방향에 맞춘다(인트로, 크레딧 연출도 옛 방향 기준) |
| 발밑 | 발바닥이 칸의 **바닥선**에 닿는다. 게임의 기준점이 발밑이다 |

---

## 2. 크기 (최종 32px 기준, 지금 게임 크기 그대로)

생성 시트에서는 이 값의 4배로 그린다. 키는 발바닥부터 머리(또는 무기 끝)까지다.

| 그림 | 최종 키(px) | 시트에서 키(px, 4배) | 비고 |
|---|---|---|---|
| 고블린(플레이어) | 64 | 256 | 2등신 개그 체형. 비율 참조 `ArtSource/Export/Player3_hd.png` |
| 전사(기사) | 65 | 260 | 칼 |
| 방패병 | 65 | 260 | 방패는 따로 한 장(아래) |
| 저격수 | 60 | 240 | 긴 총 |
| 궁수 | 52 | 208 | 활 |
| 사마귀 | 45 | 180 | 낫 팔 두 개, 옆으로 넓다 |
| 돌격병 | 77 | 308 | 덩치 |
| 골렘 3단계 | 46 / 82 / 98 | 184 / 328 / 392 | 같은 골렘이 커지는 3단계 |
| 방패병 방패 | 62 | 248 | 정면 방패만 |

---

## 3. 시트 나누기

크기 차이가 커서 한 장에 다 넣지 않는다. 아래 순서로 뽑고, **앞 시트 결과를 다음 시트의 참조 그림으로 같이 넣는다**.

1. **캐릭터 시트(시범)**: 고블린 + 일반 적 6종 + 방패. 1536×1024, 4열 × 2행, 칸 384×512, 바닥선은 칸 위에서 480px
2. 골렘 시트: 3단계
3. 소품 시트: 상자, 통, 벤치, 석상, 가로등, 꽃 조형물 등. 소품마다 지금 게임 크기의 4배
4. 건물 시트: 훈련 건물, 선술집, 성, 풍차 등. 건물 1~2개씩
5. 보스, 왕: 포즈마다 같은 캐릭터인지가 가장 어렵다. 캐릭터 시트 결과를 보고 따로 정한다
6. 바닥 타일, 울타리 조각: 반복, 이음매 문제가 있어 마지막에

칸 안에서는 그림끼리 겹치지 않게, 칸 테두리에서 16px 이상 띄운다. 칸 선은 그리지 않는다(마젠타 바탕만).

---

## 4. 받은 뒤 처리

1. 마젠타 지우기 → 칸별로 자르기 → 발밑 기준으로 위아래 여백 정리
2. 원본은 `ArtSource/Original/`에, 잘라낸 4배 그림은 `ArtSource/Export/`에
3. **지금 파일과 같은 이름**으로 `Assets/_Project/05. Art/`에 덮어쓰고, `ArtSource/Original/ppu32/`의 같은 이름 백업도 새 4배 그림으로 바꾼다
4. `pixel_ppu32.py`의 밀도 값을 128(4배)로 고치고 돌린다 → 32px 축소, 외곽선, 팔레트까지 한 번에
5. Unity에서 덮어쓴 그림이 Sprite Mode **Multiple로 자동 분할되면 Single로** 되돌린다(핸드오프 2번). PPU는 그림 크기가 바뀌면 월드 크기가 같게 다시 맞춘다
6. 게임 화면에 렌더해 지금 그림과 나란히 비교한다
7. 32px로 줄인 뒤 직접 도트를 찍어 고친 그림은 `pixel_ppu32.py`의 `HAND_EDITED`에 넣는다. 변환 스크립트를 다시 돌려도 그 그림은 덮어쓰지 않는다. 그 그림을 새로 뽑을 때만 목록에서 뺀다
   - 지금 목록: `Player3.png`(고블린, 11/6 시트 v5로 바꾼 뒤 눈, 눈썹, 입꼬리를 손봄), `King/king_smug.png`, `King/king_speechless.png`(11/6 입을 손으로 찍음), 같은 두 표정의 컷인 초상화 `King/Portrait/`(맵 그림의 입을 옮겨 찍음)

---

## 5. 받아들이는 기준 (시트마다 확인)

- [ ] 모든 그림이 같은 3/4 탑다운 시점이다
- [ ] 빛이 모두 왼쪽 위에서 온다
- [ ] 키 비율이 2번 표와 맞는다(고블린 대비 ±10%)
- [ ] 32px로 줄였을 때 얼굴, 손, 무기가 구분된다
- [ ] 팔레트를 입혀도 색이 크게 바뀌지 않는다(원래 팔레트에 가깝게 그렸다)
- [ ] 적 6종이 실루엣만 봐도 구분된다(게임 중 미리 읽기가 핵심)
- [ ] 마젠타가 그림 안으로 번지지 않았다

---

## 6. 시범 시트 프롬프트

영어가 결과가 안정적이다. 참조 그림으로 `ArtSource/palette.png`, `ArtSource/Export/Player3_hd.png`(고블린 비율), 지금 적 그림 6장을 같이 넣는다.

```
A single sprite sheet for a top-down arena action game, 1536x1024 pixels,
solid flat #FF00FF magenta background, no grid lines, no text.

Layout: 4 columns x 2 rows of equal cells (384x512 each). One character per cell,
centered horizontally, feet standing on a common ground line 32 px above the bottom
of each cell. Leave at least 16 px empty margin inside every cell.

Row 1: (1) green goblin hero, chibi 2-head proportions, brown hood, red scarf,
holding a lit round black bomb, facing right;
(2) armored knight with a sword and dark red cape;
(3) female archer with a green hood and a bow;
(4) mantis warrior with two scythe arms, wide stance.
Row 2: (5) heavy shield bearer (no shield in hand);
(6) sniper with a long rifle;
(7) big brute charger, bulky, red scarf;
(8) a single tall rectangular metal shield seen from the front.

Heights (feet to top of head/weapon): goblin 256, knight 260, archer 208, mantis 180,
shield bearer 260, sniper 240, charger 308, shield 248. Keep these relative sizes exactly.

Style rules for every sprite:
- 3/4 top-down view (camera looking down at about 45 degrees), same angle for all
- light comes from the top-left, shadows fall bottom-right
- clean pixel art look with large flat color areas, 2-3 shade steps, no gradients,
  no blur, no glow, no tiny noisy detail
- 1-pixel-wide black outline around each silhouette (at final 1/4 scale),
  inner lines in darker shades of the local color
- use only colors from the attached 64-color palette
- enemies face the viewer with the body turned slightly to the right;
  the goblin faces right
- no ground shadow, no background props, no effects
```

### 시범 결과 (11/6)

- 시트 v1(`ArtSource/Original/characters_sheet_v1.webp`)에서 고블린, 전사, 궁수, 방패병, 저격수를 게임에 넣었다(`ArtSource/apply_sheet.py`)
- 사마귀(서 있는 곤충이라 너무 가늘고 높음), 돌격병(오크라 안 됨), 방패(정면 그림이라 안 됨)는 다시 뽑는다
- 방패는 코드가 그림의 **위쪽(+Y)을 플레이어 쪽으로** 돌린다(`ShieldBearerEnemy`, 회전 = 각도 - 90). 그래서 위에서 본 방패, 바깥 면이 그림 위쪽이어야 한다

받은 결과가 기준에 안 맞으면 그 항목만 짚어 다시 시킨다. 예: "keep the same characters but make all of them the same 3/4 top-down angle as the goblin".
