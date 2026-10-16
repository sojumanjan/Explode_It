# 작업 인수인계 (2026-10-16 기준)

다른 컴퓨터·새 Claude Code 세션에서 바로 이어서 작업하기 위한 요약이다. 새 세션을 시작하면 이 파일 → `CLAUDE.md` → `Docs/design.md` 순서로 읽힌다.

> 새 세션 첫 메시지 예: "Docs/handoff.md, CLAUDE.md, Docs/design.md 읽고 이어서 하자. handoff 2번의 작업 규칙을 메모리에 저장해 줘."

---

## 1. 먼저 볼 문서

| 파일 | 내용 |
|---|---|
| `CLAUDE.md` | 프로젝트 규칙(절대 규칙, 폴더 구조, 컨벤션, 작업 방식, 현재 단계, 결정 사항) |
| `Docs/design.md` | 기획서. **무기·스킬·강화·아티팩트·업적 전체 표가 여기 있다.** CLAUDE.md와 어긋나면 이쪽이 우선 |
| `Docs/devlog.md` | AI 코드를 고치거나 버린 기록 (`날짜 \| 문제 \| 원인 \| 바꾼 방식 \| 결과`) |
| `Docs/schedule.md` | 주차별 일정. **10/8 이후 갱신 안 됨**(3주차 진행분 반영 필요) |
| `Docs/submit/` | 학원 제출용 기획서·일정 PDF |

---

## 2. 작업 규칙 (새 컴퓨터 메모리는 비어 있으니 이대로 지킨다)

### 사용자와 일하는 방식
- 답은 **한국어**. 사용자는 짧게 말한다("ㄱㄱ", "커밋 후 데브로그").
- **플레이 테스트는 사용자가 한다.** 플레이모드에 들어가지 않는다. 작업 끝에 "바꾼 것 / 에디터에서 할 일 / 확인 방법"을 짧게 정리한다.
- **MCP 작업 전 플레이 중인지 먼저 확인**(`EditorApplication.isPlaying`). 플레이 중에 컴파일하면 실행 중인 게임이 깨지고(NullReference), 씬 저장도 실패한다. 플레이 중이면 꺼 달라고 한다.
- **에디터 작업:** 원칙은 인스펙터 안내지만, 사용자가 "너가 해줘 / ㄱㄱ"라고 하면 Unity MCP `execute_code`로 씬·프리팹·SO·임포트 설정을 직접 고쳐 왔다(지금은 거의 항상 직접 한다).
- **큰 변경(파일 3개 이상, 구조 변경, 새 시스템)은 계획을 먼저 말하고 확인받는다.** 패키지 추가는 먼저 묻는다. `.meta`는 직접 만들거나 고치지 않는다.
- **SO 필드에는 모두 한글 `[Tooltip]`**(의미 + 단위). 밸런스 수치는 코드에 하드코딩하지 않는다.
- **형제 클래스도 같이 고친다.** 한 적의 문제를 고치면 같은 구조의 다른 적도 묻지 말고 점검·수정하고 무엇을 같이 바꿨는지 알린다.
- 주석은 한글로 "왜"를 적는다. `[SerializeField] private`, `_camelCase`, 한 파일 한 클래스.
- 사용자가 에셋 수치를 인스펙터에서 자주 직접 바꾼다. 문서에 수치를 적을 땐 기억 말고 **에셋에서 뽑아서** 적는다.

### 커밋과 데브로그
- **커밋은 요청이 있을 때만.** "커밋 후 데브로그" → 기능 커밋 1개 + 데브로그 커밋 1개.
- 메시지: `[Feat]/[Fix]/[Docs]/[Refactor] 한 줄 요약` (+ 빈 줄 + `- 항목`) + 마지막 줄 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`
- 사용자가 직접 바꾼 데이터·그림도 같이 커밋하고 메시지/보고에 언급한다.
- 데브로그는 AI가 만든 것을 고치거나 버렸을 때만 `Docs/devlog.md` 끝에 한 줄. 경로는 대문자 `Docs/`(git이 대소문자 구분).
- 푸시는 사용자가 말할 때만.

### Unity MCP 사용 팁
- `execute_code`는 `compiler: codedom`(C# 6). `Object`는 `UnityEngine.Object`로 명시. `objectReferenceInstanceIDValue`는 막힘(obsolete).
- `AssetDatabase.DeleteAsset`, `File.Delete`는 안전 검사에 막힌다 → 에셋 삭제는 참조 확인 후 `git rm`.
- 스크립트 수정 후 `refresh_unity(compile: request, mode: force)` → `read_console(types: error, warning)`. 새 타입이 정말 잡혔는지 리플렉션으로 한 번 확인.
- **`execute_code`의 `replay`는 쓰지 않는다.** 번호가 헷갈려 옛 코드가 다시 돌아간 적이 있다(폰트 값이 되돌아감).
- 씬 오브젝트는 **이름이 아니라 전체 경로로** 찾는다. 같은 이름(`Goblin`)이 다른 페이지에도 있어 엉뚱한 걸 고친 적이 있다.
- 프리팹은 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`.
- UI 확인은 임시 카메라 + `ScreenSpaceCamera`로 캔버스를 렌더해 scratchpad에 PNG로 저장해서 본다(끝나면 상태 복구).
- MCP 연결이 끊기면(CONNECT_TIMEOUT) 사용자에게 재연결을 부탁한다. 에디터가 뒤에 가려져 있으면 컴파일이 멈추므로 Unity를 한 번 클릭해 달라고 한다.

### 셸 주의
- Git Bash 히어독(`<<'EOF'`) 안의 파이썬에서 `\\n`이 줄바꿈으로 바뀌어 C# 문자열이 깨진다. 이스케이프가 들어가는 수정은 **파이썬 스크립트를 파일로 써서** 실행한다.
- 사용자 PC 파이썬은 `-I`(격리)로 돌리면 PIL을 못 찾는다. 이미지 처리 스크립트는 scratchpad에서 그냥 실행.

### 그림 가져오기 주의 (반복된 문제)
- 사용자가 같은 이름으로 그림을 덮어쓰면 Unity가 **Sprite Mode Multiple로 자동 분할**해 참조가 끊긴다. 넣을 때마다 Single로 되돌리고 다시 연결한다.
- 캐릭터·왕: Single, Point, 발밑 기준점, PPU 100, 압축 없음. 각성 왕 동작 그림 일부 PPU 80.
- 폭탄 그림(`bomb_*.png`): Single, 기준점은 공 몸통 가운데(불투명 픽셀 무게중심). UI에서는 `PivotCenteredImage`가 기준점을 반영한다.
- **이미지 생성 모델은 투명 배경 대신 바둑판 무늬를 그려 넣는다.** 배경 제거·분할은 파이썬(PIL)으로 했다(밝은 무채색 덩어리 제거 → 물건별 덩어리로 잘라냄). 다음엔 프롬프트에 `solid flat #FF00FF magenta background`를 넣게 안내.

### 정렬 규칙 (10/8 도입)
- 2D 렌더러 Transparency Sort Mode = Custom Axis (0, 1, 0) → 화면 아래쪽이 앞.
- 서 있는 것 = Default 레이어 순서 5, Sprite Sort Point = Pivot, 기준점 발밑. 그림자 1, 범위·예고선 0~3, 투사체·이펙트 6+, 대화 왕 20.
- 대각선 소품은 세로 띠로 나눠 띠마다 기준점(`Slice_N`).

---

## 3. 씬과 화면 흐름

| 씬 | 내용 |
|---|---|
| `04. Scenes/Title.unity` | **타이틀**(첫 화면: 시작하기·설정·나가기) + **메인**(책 메뉴). 한 씬 안에서 `MenuPager`가 페이지를 넘긴다 |
| `04. Scenes/Game.unity` | 실제 플레이. 이름이 예전엔 Main.unity였다 |

- 용어: **"타이틀" = 첫 화면, "메인" = 책 메뉴.** 헷갈려서 이름을 정리했다.
- 타이틀 → 메인: 1.5초 검게 덮기 → 0.5초 밝아지기, BGM도 같이(`Bgm_Title` → `Bgm_Main`). 설정은 페이지 넘김 없이 타이틀 위에 책만 띄운다(덮개 열기).
- 메인(책): 왼쪽 장비 칸(무기 · 스킬 · 아티팩트 I/II) + 모드 버튼(스토리, 무한은 잠김). 장비 칸을 누르면 `SelectPage`(고르기 페이지).
- 게임 → 메인: 결과 창 버튼이 검게 덮고 씬을 넘김, 메인에서 0.8초 밝아짐(타이틀은 건너뜀).
- 판 시작: `RunLauncher` → `RunSetup.Set(모드, 무기, 스킬, 아티팩트)` → Game 씬. Game 씬의 `RunLoadoutApplier`가 무기를 끼우고 아티팩트를 판 보정값에 채운다.

---

## 4. 시스템 지도 (어디를 보면 되는지)

### 저장
- `Core/SaveSystem.cs`, `SaveData.cs`, `SaveStat.cs`: JSON 한 덩어리를 PlayerPrefs에 저장. 해금·장착(무기·스킬·아티팩트 칸·강화)·업적 달성·누적 기록(이름 붙은 Stat)·설정.
- `Progression/ProgressRecorder.cs`: 판이 끝날 때 누적 처치·기록 저장.

### 해금 구조
- `Core/IUnlockable.cs`: 무기·스킬·강화·아티팩트 공통 인터페이스(Id, 이름, 설명, 아이콘, 해금 여부).
- `Progression/UnlockCatalog.cs` (+ `02. Data/Progression/UnlockCatalog.asset`): 모든 무기·스킬·강화·아티팩트·업적 목록. **고르기 화면 카드 순서 = 여기 목록 순서.**
- `Progression/AchievementData.cs`: 조건 + 목표 + (스킬 지정) + 보상(무기·스킬·강화·아티팩트 각각 하나씩 여러 개 가능) + 스티커 그림.
- `Progression/AchievementCondition.cs`: 조건 종류. **새 조건은 맨 뒤에만 추가**(에셋이 번호로 기억).
- `Progression/AchievementTracker.cs`: 판 도중 조건을 세고 달성 시 해금·저장. 개발자 패널을 쓴 판은 세지 않음.
- 처치 출처: `Core/DamageSource.cs`(Bomb·BlackHole·ScatterBomb·InstantBomb·Dodge), 적 부류 `Core/EnemyKind.cs`(Normal·ShieldBearer·Boss). `GameEvents.EnemyKilledBy(source, kind)`.

### 무기
- `Bomb/WeaponData.cs`(에셋 `02. Data/Weapons/`), `BombLauncher.cs`, `Bomb.cs`. 무기 4종(기본·빠른연사·큰폭발·폭탄3개). 더블배럴은 보류(목록에서 빠짐).
- **클릭 한 번에 폭탄 하나**(꾹 눌러 연사 없음). 던지기 간격 직전 클릭은 0.15초 기억.
- 대시 중에는 못 던진다(빨간 리본으로 허용).

### 스킬 (우클릭)
- `Progression/PlayerAbility.cs`, `AbilityData.cs`(에셋 `02. Data/Progression/Abilities/`), `AbilityKind.cs`.
- 블랙홀(기본) / 다발폭탄(한 발은 발밑, 나머지 사방, 시각·거리·폭발 무작위, 벽 무시) / 순간폭탄(일정 시간 착지 즉시 폭발·공속 배율, 화면 테두리 `UI/InstantBombBorder.cs`). 스킬은 모두 방패 관통.
- 강화: `AbilityUpgradeData.cs`(에셋 `Upgrades/`), 효과 `AbilityUpgradeEffect.cs`. **해금 + 장착해야 적용.**

### 아티팩트 (2칸)
- `Progression/ArtifactData.cs`(효과 종류 + 값1 + 값2, 에셋 `02. Data/Progression/Artifacts/`), `ArtifactEffect.cs`.
- 판 시작 시 `RunLoadoutApplier`(실행 순서 -200)가 `Core/RunModifiers.cs`(정적 판 보정값)에 모으고, 각 시스템이 읽는다:
  이동속도(`PlayerMovement`), 대시 횟수·쿨·대시 처치(`PlayerDodge`), 체력(`PlayerDeath`), 스폰 간격(`StageRunner`), 게이지 칸·필요 처치·동전·반지(`PlayerAbility`), 대시 중 던지기(`BombLauncher`).
- 아이콘 `05. Art/Artifacts/Artifact_*.png`.

### 체력·대시
- `Player/PlayerDeath.cs`: 스토리 2칸(+팬던트), 무한 1칸. 피격 후 반투명 무적. 사망 연출(찌그러짐 → 사망 그림 → 카메라 확대 → 슬로모션 → 결과 창).
- `Player/PlayerDodge.cs`, `DodgeData.cs`: 대시 충전식(기본 1회), 대시 무적으로 공격을 피하면 슬로모션 + `GameEvents.PlayerEvaded`(회피 업적).

### UI
- 메뉴: `MenuPager`, `TitlePage`, `MainMenuPage`, `EquipSelectPage`(고르기), `ItemCard`(카드), `SettingsPage`, `ConfirmDialog`, `ScreenFader`, `HoverScale`(버튼 1.1배), `HoverLabel`(마우스 올리면 "장착 해제").
- 고르기 페이지 규칙: 노란 표시 = 지금 장착 중만. 다른 아티팩트 칸에 낀 것은 회색(눌러서 설명만, "장착 중" 고정). "장착 중"을 다시 누르면 해제(아티팩트·강화만, 무기·스킬은 바꿔 끼기만). 카드 더블클릭 = 장착/해제. 장착해도 페이지에 남는다(뒤로가기로만 메인).
- 해금한 아티팩트를 누르면 해금 조건(취소선) + CLEAR 도장(`05. Art/UI/Stamp_Clear.png`, 조건 글 끝에 따라 붙음) + 능력.
- **고르기 카드는 씬에 미리 놓여 있다**(`SelectPage` 아래 무기 4·아티팩트 10, `AbilityList` 아래 스킬 줄 3개 × 카드 + 강화 칸 2). 아이템을 추가하면 카드를 복제해 같은 순서에 넣고 `EquipSelectPage` 배열에도 넣는다.
- 게임 중: `AchievementToast`(좌측 하단, 업적 이름 + 달성 방법, 4초), `UnlockSummaryPanel`(판이 끝나면 해금 창, 보상마다 한 장씩 넘김, 획득 경로 = 업적 설명 + 스티커), `StatePanel`(결과 창: 다 보일 때까지 클릭 막음, 뜨면 게임 소리 끔), `HealthView`, `AbilityGaugeView`(칸 수에 맞춰 폭 나눔).
- 소리: `Core/AudioManager.cs`. `SoundData.IsInterface`가 켜진 소리(클릭·에러·해금·대사)만 결과 화면에서도 들린다. 소리 겹침 상한·간격은 소리마다.

### 개발자 패널 (`UI/DevPanel.cs`, Game 씬)
- 무기 교체·수치 조정, 보스 바로가기(Final Boss, King Groggy/Defeat), Replay Intro, **Unlock All**(무기·스킬·강화·아티팩트 전부), **Reset Achv**(업적·해금·장착만 초기화), Log Save, Reset Save, Fill Ability, Kill All, 배속.
- 패널을 쓴 판은 업적을 세지 않는다.

---

## 5. 콘텐츠 현황 (숫자는 design.md 표가 기준)

- 스토리: 웨이브 4개(각 100마리) + 보스 4전(화염 마법사 → 드릴 광부 → 야수 → 각성한 왕) → 400킬 엔딩.
- 적: 전사, 궁수, 저격수, 사마귀, 방패병, 돌격병.
- 최종 보스(`Enemy/Boss/AwakenedKingBoss.cs`): 3국면 패턴(1국면 베기·돌진·왕관 부메랑 / 2국면 베기·도약 내려찍기·왕관 투하 / 3국면 유성우·천지 가르기·별의 의식)과 국면 전환 연출이 코드에 있다. 도약은 플레이어 기준 위쪽 150도 공중 자리 → 상단 30도 안이면 수직, 아니면 대각 내려찍기.
- 업적 19개(전부 보상 있음) / 무기 4 / 스킬 3 / 강화 6 / 아티팩트 10.
- 스티커 그림 없음 → 공용 빈 스티커(`05. Art/UI/Sticker_Empty.png`)로 대신.

---

## 6. 남은 일

- [ ] **도감(스티커) 페이지**: 지금 있는 해금 업적 19개를 모두 도감에 넣기로 함. 이후 수치를 바꾼 단계형(예: 처치 수 단계)이나 새 수집 요소로 늘릴 예정
- [ ] 업적 목록 확정 후 **스티커 시트 한 장으로 생성**(프롬프트 틀은 대화에서 만듦: 4×3 격자, 흰 테두리 스티커, 글자 없음)
- [ ] 무한 모드(절차적 웨이브, 보스 로테이션), 스토리 클리어 → 무한 해금 연결
- [ ] 승천(하드모드)
- [ ] `Docs/schedule.md` 갱신
- [ ] 해금 창을 보상마다 한 장 → 업적당 한 장으로 묶을지(사용자 답 대기)
- [ ] 무기·스킬 고르기에도 해금 조건 + CLEAR 도장을 보일지(지금은 아티팩트만)
- [ ] 엔딩 연출, 최종 보스 전용 사운드, WebGL 빌드 확인, 맵 아트(평화로운 중세 마을 컨셉) 계속
- 정리할 것: 안 쓰는 `07. Materials/SpriteSilhouette.shader`, `Mat_Silhouette*.mat` / 외부 에셋 팩은 `09. ThirdParty`로 옮기는 게 규칙상 맞음

---

## 7. 새 컴퓨터 준비

1. 저장소 pull/clone 후 Unity **6000.5.6f1**로 열기(2D URP, Input System, Cinemachine 3, DOTween).
2. Unity MCP(MCP for Unity) 연결 확인 — Claude Code에서 `mcp__UnityMCP__*` 도구가 보여야 한다.
3. 저장 데이터는 PlayerPrefs라 **컴퓨터마다 따로**다. 집에서는 해금이 비어 있으니 개발자 패널 `Unlock All`로 시작하면 편하다.
4. 새 세션에 위 첫 메시지 예시를 보내고, 2번 규칙을 메모리에 저장하게 한다.
