# 작업 인수인계 (2026-10-25 기준)

다른 컴퓨터·새 Claude Code 세션에서 바로 이어서 작업하기 위한 요약이다. 새 세션을 시작하면 이 파일 → `CLAUDE.md` → `Docs/design.md` 순서로 읽힌다.

> 새 세션 첫 메시지 예: "Docs/handoff.md, CLAUDE.md, Docs/design.md 읽고 이어서 하자. handoff 2번의 작업 규칙을 메모리에 저장해 줘."

---

## 0. 최근(10/24~25)에 한 것과 다음에 할 것

### 다음에 바로 할 것 (우선순위 순)
1. **웹 빌드 → itch.io 배포** ← 1순위
   - 웹 설정은 10/25에 맞춰 두었다: Brotli + Decompression Fallback(itch.io 호환), 기본 화면 1280x720, IL2CPP 코드 크기 최적화, 스트리핑 Minimal, 스플래시·로고 끔.
   - Build Profiles에 Web 프로필을 사용자가 추가해 두었다(`Assets/Settings/Build Profiles/Web.asset`).
   - 빌드 → 로컬에서 확인 → itch.io에 HTML로 올림(1280x720, 전체화면 버튼 켬) → 웹에서도 플레이 기록이 들어오는지 대시보드 확인.
   - 빌드 리포트로 최종 용량을 다시 잰다(Windows 10/24: 262MB → 230MB, 이후 텍스처 압축 176장은 아직 안 잼). 포트폴리오에 쓸 숫자.
2. **찐엔딩 크레딧 실제 플레이 확인**: 곡(49.2초, 반복 끔)이 끝나는 순간과 장면·크레딧 글이 끝나는 순간(47.9초, 보라 덮기 시작)이 맞는지. 개발자 패널 Unlock All → 저주 9개 + 왕관 → 무한 모드.
3. 시간이 되면: 남은 효과음, UI 디자인(미정), 맵 아트.

### 10/24~25에 끝낸 것
- **플레이 기록**: 이벤트 7종 수신 확인, `boss_ended` 대시보드 등록, Send From Editor 끔. 동의 창 문구를 간단하게(플레이 기록 + 기본 기기 정보, 익명, 설정에서 끔), 상자 크기 맞춤.
- **업적 30개**: 저주 업적 3종(저주받은 자 = 저주 9개 해금, 하늘의 도움 = 운석·낙뢰 누적 50, 붉은 사냥 = 광폭 적 누적 50) + 진짜 끝 도감 자리(2면 오른쪽에 크게). 새 스티커 4장(원본 `ArtSource/Original/stickers/`), 실루엣 12장 매끈하게 다시.
- 무기·스킬·강화 고르기에도 해금 조건 줄긋기 + CLEAR 도장(아티팩트와 같게).
- **빌드 용량**: 안 쓰는 스프라이트·오디오·외부 에셋 팩·실루엣 셰이더·정적 글꼴·SampleScene 삭제, BGM 품질 50%, 스플래시·유니티 로고 끔, Visual Scripting 제거, 맵 배치 크기 기준으로 텍스처 23장 최대 크기 낮춤, 200KB 이상 텍스처 176장 고품질 압축. 가로세로가 4의 배수가 아니면 압축이 안 돼서 78장을 투명 픽셀로 덧댐(피벗 보정). 밉맵 켜진 2의 거듭제곱 아닌 스프라이트도 압축이 안 됨 → 밉맵 끔.
- **스킬**: 블랙홀 범위는 무기와 상관없이 3.74 고정(범위 강화만 곱함). 순간폭탄 사거리 1.5배 추가, 설명 "공격 속도 4배, 사거리 1.5배".
- **버그**: 히트스톱 도중 상태가 바뀌면 효과음이 늘어진 채 남던 것(클릭음) 수정. 각성 왕 `OnDisable`의 낙하 표시 오류 null 확인.
- **크레딧**: 4장면을 '왕의 행차'로 교체(쪼꼬미 왕이 레드카펫을 통통 걸어옴 → 기사 6명 경례 → 바들바들 → 각성 → 검 → 4초 기 모으기). 마지막 장면이 끝나면 보스 3국면 필살기 보라로 1.5초 덮고 그 위에 마지막 문구(짙은 보라). 3장면 야수 머리에 빠직(💢). 크레딧 곡 새로(`Bgm_Credits` 68.5초, 반복 끔). 글 속도는 시작할 때 실제 글 높이와 곡 길이로 자동 계산해, 마지막 줄이 빠지는 순간 곡이 '보라 1.5 + 문구 떠오르기 1.5 + 머무는 3.5초'만 남게 한다. 곡 끝 0.3초 전에 문구·보라를 걷고 까만 화면, 마지막 0.05초만 페이드(끝 소리 살림). 빨리 감기·건너뛰기(스페이스 1초 길게 누르기, 게이지)를 했으면 보라가 시작될 때 곡을 끝부분으로 옮긴다. 장면 사이 까만 간격 2초, 장면 페이드 1.2초.
- 타이틀 로고 그림(`05. Art/UI/TitleLogo.png`, 예전 글자 제목은 꺼 둠). 버튼 28개에 테두리·하이라이트·그림자를 더한 9-슬라이스(`05. Art/UI/Buttons/Button_Base.png`, 색은 그대로 곱함). 타이틀 버튼이 페이지 넘길 때 반투명해지던 것 수정. 도감 이전/다음은 못 넘기면 숨김.
- 일시정지 창 닫을 때 왼쪽으로 쏙 빠짐. 편지 꺼내는 소리(`Sfx_LetterOpen`). 왕관 저주 막힘 문구 "모든 저주 장착 필요".
- 30초 소개 영상 완성(저장소 밖). Unity Recorder 패키지 추가.
- 나무판 버튼(9-슬라이스)을 시도했다가 사용자 판단으로 원상복구.

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
- 답은 **한국어**. 사용자는 짧게 말한다("ㄱㄱ", "데브로그 후 커밋"). **작업 중간 진행 메모도 한국어로**(영어로 쓰면 진행 상황을 따라가기 어렵다는 지적이 있었다).
- **플레이 테스트는 사용자가 한다.** 플레이모드에 들어가지 않는다. 작업 끝에 "바꾼 것 / 에디터에서 할 일 / 확인 방법"을 짧게 정리한다.
- **MCP 작업 전 플레이 중인지 먼저 확인**(`EditorApplication.isPlaying`). 플레이 중에 컴파일하면 실행 중인 게임이 깨지고(NullReference), 씬 저장도 실패한다. 플레이 중이면 꺼 달라고 한다.
- **에디터 작업:** 원칙은 인스펙터 안내지만, 사용자가 "너가 해줘 / ㄱㄱ"라고 하면 Unity MCP `execute_code`로 씬·프리팹·SO·임포트 설정을 직접 고친다(지금은 거의 항상 직접 한다).
- 씬이 이미 dirty(사용자가 저장 안 한 변경)면 MCP로 그 씬을 저장하지 않는다. 수정만 해 두고 사용자에게 저장(Ctrl+S)을 부탁한다.
- **큰 변경(파일 3개 이상, 구조 변경, 새 시스템)은 계획을 먼저 말하고 확인받는다.** 패키지 추가는 먼저 묻는다. `.meta`는 직접 만들거나 고치지 않는다.
- **SO 필드에는 모두 한글 `[Tooltip]`**(의미 + 단위). 밸런스 수치는 코드에 하드코딩하지 않는다.
- **형제 클래스도 같이 고친다.** 한 적의 문제를 고치면 같은 구조의 다른 적도 묻지 말고 점검·수정하고, 무엇을 같이 바꿨는지 알린다.
- 주석은 한글로 "왜"를 적는다. `[SerializeField] private`, `_camelCase`, 한 파일 한 클래스.
- 사용자가 에셋 수치를 인스펙터에서 자주 직접 바꾼다. 문서에 수치를 적을 땐 기억 말고 **에셋에서 뽑아서** 적는다.

### 커밋과 데브로그
- **커밋은 요청이 있을 때만.** "커밋 후 데브로그" / "데브로그 후 커밋"이면 **데브로그를 먼저 쓰고 작업과 한 커밋으로** 묶는다. 따로 커밋하지 않는다(10/21 사용자 지시).
- 메시지: `[Feat]/[Fix]/[Docs]/[Refactor]/[Art]/[Audio] 한 줄 요약` (+ 빈 줄 + `- 항목`) + 마지막 줄 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`
- 사용자가 직접 바꾼 데이터·그림도 같이 커밋하고, 메시지와 보고에 언급한다.
- 폰트 에셋 `Assets/_Project/08. Fonts/BMJUA_Dynamic.asset`은 늘 바뀌어 있지만 **커밋에서 뺀다**.
- 데브로그는 AI가 만든 것을 고치거나 버렸을 때만 `Docs/devlog.md` 끝에 한 줄 쓴다. 경로는 대문자 `Docs/`(git이 대소문자를 구분한다).
- 푸시는 사용자가 말할 때만.

### Unity MCP 사용 팁
- `execute_code`는 `compiler: codedom`(C# 6). `Object`는 `UnityEngine.Object`로 명시한다. `objectReferenceInstanceIDValue`는 막혀 있다(obsolete).
- `AssetDatabase.DeleteAsset`, `File.Delete`는 안전 검사에 막힌다 → 에셋 삭제는 참조 확인 후 `git rm`.
- 스크립트 수정 후 `refresh_unity(compile: request, mode: force)` → `read_console(types: error, warning)`. 새 타입이 정말 잡혔는지 리플렉션으로 한 번 확인한다.
- **`execute_code`의 `replay`는 쓰지 않는다.** 번호가 헷갈려 옛 코드가 다시 돌아간 적이 있다(폰트 값이 되돌아감).
- 씬 오브젝트는 **이름이 아니라 전체 경로로** 찾는다. 같은 이름(`Goblin`)이 다른 페이지에도 있어 엉뚱한 걸 고친 적이 있다.
- 다른 씬을 고칠 땐 `OpenScene(Additive)` → 수정 → `SaveScene` → `CloseScene(true)`.
- 프리팹은 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`.
- UI 확인은 임시 카메라 + `ScreenSpaceCamera`로 캔버스를 렌더해 scratchpad에 PNG로 저장해서 본다.
- **미리보기 렌더에서 바꾼 값은 같은 코드의 finally에서 직접 되돌린다.** 10/23에 사용자가 Game 씬을 단독으로 열어 둔 상태에서 같은 씬을 추가로 열고 `CloseScene(false)`로 버리려다, 닫히지 않아 변경(Canvas 끄기 등)이 다음 작업에서 저장된 사고가 있었다. 미리보기 전에 대상 씬이 이미 열려 있는지(`SceneManager.GetSceneAt`) 확인한다.
- MCP 연결이 끊기면(CONNECT_TIMEOUT) 사용자에게 재연결을 부탁한다. 에디터가 뒤에 가려져 있으면 컴파일이 멈추므로 Unity를 한 번 클릭해 달라고 한다.

### 셸 주의
- Git Bash 히어독(`<<'EOF'`) 안의 파이썬에서 `\\n`이 줄바꿈으로 바뀌어 C# 문자열이 깨진다. 이스케이프나 따옴표가 많은 수정은 **파이썬 스크립트를 파일로 써서** 실행하거나 Edit/Write 도구를 쓴다.
- 사용자 PC 파이썬은 `-I`(격리)로 돌리면 PIL을 못 찾는다. 이미지 처리 스크립트는 그냥 실행한다(PIL, numpy, scipy 사용. 다른 PC에 scipy가 없으면 설치를 묻는다).

### 그림 가져오기 주의 (반복된 문제)
- 사용자가 같은 이름으로 그림을 덮어쓰면 Unity가 **Sprite Mode Multiple로 자동 분할**해 참조가 끊긴다. 넣을 때마다 Single로 되돌리고 다시 연결한다.
- 캐릭터·왕: Single, Point, 발밑 기준점, PPU 100, 압축 없음. 각성 왕 동작 그림 일부는 PPU 80.
- 폭탄 그림(`bomb_*.png`, `블랙홀폭탄.png`): Single, 기준점은 공 몸통 가운데.
- **이미지 생성 모델은 투명 배경 대신 바둑판 무늬를 그려 넣는다.** 그래서 프롬프트에 `solid flat #FF00FF magenta background`를 넣게 안내한다. 받은 그림은 파이썬으로 마젠타를 지우고 덩어리별로 잘라낸다. 어두운 배경(벽돌 등)이면 채도·밝기로 분리한다.
- 이미 픽셀로 그려진 그림은 원래 격자로 되돌려 쓴다(칸 크기 찾기 → 칸 가운데 색).
  - 전체 화면 그림(타이틀·컷씬·엔딩): 240×135
  - 캐릭터: 기존 그림 크기(예: 100×100)
  - Point 필터, 압축 없음
- 그림 처리 원본 백업은 `ArtSource/Original/`, 처리 결과는 `ArtSource/Export/`. 처리 스크립트는 `ArtSource/pixel_restore.py`, `ArtSource/cutscene.py`.
- 이미지 생성은 사용자가 **바르코**로 한다(여러 장 참조 편집). 고블린 비율은 `ArtSource/Export/Player3_hd.png`를 참조로 넣어야 애기 체형이 안 된다.
- 폰트(BMJUA)에 **가운뎃점(·)이 없다.** 화면 글에는 "/", ",", "-"를 쓴다.

### 정렬 규칙 (10/8 도입)
- 2D 렌더러 Transparency Sort Mode = Custom Axis (0, 1, 0) → 화면 아래쪽이 앞.
- 서 있는 것 = Default 레이어 순서 5, Sprite Sort Point = Pivot, 기준점 발밑. 그림자 1, 범위·예고선 0~3, 투사체·이펙트 6+, 대화 왕 20.
- 대각선 소품은 세로 띠로 나눠 띠마다 기준점(`Slice_N`).
- 크레딧 장면 배우도 바닥을 걸으면 순서 5(높이로 앞뒤가 정해짐). 소파 위 왕·고블린만 10.

---

## 3. 씬과 화면 흐름

| 씬 | 내용 |
|---|---|
| `04. Scenes/Title.unity` | **타이틀**(첫 화면: 시작하기·설정·나가기) + **메인**(책 메뉴). 한 씬 안에서 `MenuPager`가 페이지를 넘긴다 |
| `04. Scenes/Game.unity` | 실제 플레이. 정원(인트로, y≈63)과 분수대 광장(x≈-69, 크레딧 광부 장면)도 이 씬 안에 있다 |

- 용어: **"타이틀" = 첫 화면, "메인" = 책 메뉴.**
- 처음 켤 때 타이틀 위에 플레이 기록 동의 창(`AnalyticsConsentDialog`)이 한 번 뜬다. 답은 저장되고 설정에서 바꿀 수 있다.
- 타이틀 → 메인: 1.5초 검게 덮기 → 0.5초 밝아지기, BGM도 같이(`Bgm_Title` → `Bgm_Main`). 설정은 페이지 넘김 없이 타이틀 위에 책만 띄운다.
- 메인(책): 왼쪽 장비 칸(무기 · 스킬 · 아티팩트 I/II) + 오른쪽 모드(스토리, 무한, 저주 칸).
- 판 시작: 모드 버튼 → 기록 창(`ModeRecordPopup`)의 시작 → `RunLauncher` → `RunSetup.Set(모드, 무기, 스킬, 아티팩트, 저주)` → Game 씬. Game 씬의 `RunLoadoutApplier`가 무기를 끼우고 아티팩트·저주를 판 보정값에 채운다.
- 첫 판: 타이틀 "시작하기" → 바로 오프닝 만화 → 정원 → 튜토리얼 → 아레나(첫 결과 창을 보기 전까지는 메인을 건너뛴다).
- 스토리: 최종 보스를 쓰러뜨리면 처치 대화 → 엔딩(만화·편지) → 결과 창.
- 저주 9개 + 왕관으로 무한 모드에 들어가면 찐엔딩 판(4번 "찐엔딩·크레딧").
- 게임 → 메인: 결과 창 버튼이 검게 덮고 씬을 넘긴다. 메인은 0.8초에 걸쳐 밝아진다(타이틀은 건너뜀).

---

## 4. 시스템 지도 (어디를 보면 되는지)

### 저장
- `Core/SaveSystem.cs`, `SaveData.cs`(버전 2), `SaveStat.cs`: JSON 한 덩어리를 PlayerPrefs에 저장한다.
- 저장하는 것: 해금, 장착(무기·스킬·아티팩트 칸·강화·저주), 업적 달성, 누적 기록, 설정, 첫 판 여부, 플레이 기록 동의, 판 수, 총 플레이 시간.
- `Progression/ProgressRecorder.cs`: 판이 끝날 때 누적 처치·최고 기록을 저장한다(찐엔딩 판은 기록 안 함).

### 해금 구조
- `Core/IUnlockable.cs`: 무기·스킬·강화·아티팩트·저주 공통 인터페이스.
- `Progression/UnlockCatalog.cs` (+ `02. Data/Progression/UnlockCatalog.asset`): 모든 무기·스킬·강화·아티팩트·업적·저주 목록. **고르기 화면 카드 순서 = 여기 목록 순서.**
- `Progression/AchievementData.cs`, `AchievementCondition.cs`(**새 조건은 맨 뒤에만 추가**), `AchievementTracker.cs`.
- 처치 출처 `Core/DamageSource.cs`, 적 부류 `Core/EnemyKind.cs`, `GameEvents.EnemyKilledBy(source, kind)`.

### 무기 · 스킬 · 아티팩트 · 체력
- 무기 `Bomb/WeaponData.cs`(4종), 클릭 한 번에 폭탄 하나. 대시 중에는 못 던진다(빨간 리본으로 허용).
- 스킬 `Progression/PlayerAbility.cs`, `AbilityData.cs`: 블랙홀 / 다발폭탄 / 순간폭탄. 강화는 해금 + 장착해야 적용된다.
- 아티팩트 2칸 `Progression/ArtifactData.cs` → 판 시작 시 `Core/RunModifiers.cs`에 모아 각 시스템이 읽는다.
- 체력 `Player/PlayerDeath.cs`: 2칸(+팬던트), 저주 "한 방"이면 1칸. 대시 `Player/PlayerDodge.cs`.

### 저주 (무한 모드 전용)
- `Progression/CurseData.cs`(저주마다 다른 SO 클래스, 에셋 `02. Data/Curses/`), 판 상태 `Core/RunCurses.cs`, 장면 동작은 `CurseBehaviour<T>` 상속 클래스들(Game 씬 `Curses` 오브젝트). 조작이 잠기면(대화·연출) 저주도 멈춘다.
- 저주 고르기 `UI/CursePage.cs`. 해금은 `CurseUnlockTracker`(N개 이상 끼고 100마리).

### 찐엔딩 · 크레딧
- `Stage/TrueEndingData`: 결전 대화, 처치 후 확대·페이드, 크레딧 줄 목록(`CreditsEntry`), 글 속도, 마지막 보라 덮기·문구, 첫 장면 지연(2.5초). 크레딧 곡은 반복을 끈 `BgmData`(Loop 칸).
- `Stage/TrueEndingSequence`: Game 씬 `TrueEnding` 오브젝트, UI는 `Canvas/Credits`(오른쪽 크레딧 + 그러데이션, 왼쪽 장면, 덮개).
- 왼쪽 장면은 `CreditsShot`(씬) + `CreditsShotData`(대본). 대본의 횟수 칸은 콩콩 뛰기 횟수이자 떨기의 흔들림 수 배율(길게 떨면 초만큼). 마지막 장면은 까맣게 끊지 않고 보라로 덮인다. 크레딧 곡 길이를 바꾸면 장면 길이 합·글 속도·보라 시작 시각을 다시 맞춘다. 장면 추가 방법: `CreditsShots` 아래 장면 복제 → 배우·자리 바꾸기 → 대본 SO 새로 만들기 → `TrueEnding`의 장면 목록에 넣기.

### 플레이 기록 (Unity Analytics 6.3.0, `Core/Analytics/`)
- `PlayAnalytics`(정적 창구): 동의했을 때만 `UnityServices.InitializeAsync` + `EndUserConsent.SetConsentState(AnalyticsIntent = Granted)`. 동의를 거두면 Denied. 6.3에서 `StartDataCollection`은 폐기됐다.
- `AnalyticsBootstrap`: Title·Game 씬의 `Analytics` 오브젝트에 있다.
- `RunAnalytics`(Game 씬): 게임 이벤트를 듣고 보낸다. 판 끝에 Flush.
- 설정 `AnalyticsData`(`02. Data/AnalyticsData.asset`): 전체 켜기 / 에디터에서도 보내기 / 콘솔에 찍기.
- 동의 창 `UI/AnalyticsConsentDialog`(Title `Canvas/ConsentDialog`), 설정 `SettingsPage`의 "기록 보내기".
- 이벤트는 대시보드 Event Manager에 등록·활성화까지 끝냈다(파라미터 39개). **이름·타입이 코드와 다르면 그 이벤트가 통째로 버려진다.**
  - `run_started`: run_id, mode, is_first_run, weapon, ability, artifact_1, artifact_2, upgrade_count, curse_count, curses
  - `run_ended`: run_id, mode, result(clear/death/abandoned), survival_sec, kills, wave_reached, boss_reached, killer, hits_taken, accuracy, ability_uses, ability_kills, dash_count, weapon, curse_count, run_index, total_play_sec, story_cleared, unlocked_count
  - `boss_started`: run_id, boss, boss_number, lap, run_sec
  - `boss_ended`: run_id, boss, result(defeated/died), fight_sec, hits_taken
  - `player_hit`: run_id, source, source_kind(enemy/boss/curse), wave, run_sec, hp_left
  - `tutorial_step`: step_name, elapsed_sec
  - `ending_seen`: run_id, ending(story/true), credits_skipped, fast_forward_ratio
  - 타입: is_first_run·story_cleared·credits_skipped = BOOLEAN, fast_forward_ratio = FLOAT, 이름·문자 = STRING, 나머지 수 = INTEGER. 적·저주 이름은 데이터 에셋 이름(영문)이다.
- 표본이 100명 안팎이라 평균보다 **개인별 흐름**(userID로 걸러 시간순, run_id로 판 묶기)을 보는 게 목적이다.
- 새 항목을 넣으려면 코드에 값을 넣고, 대시보드에도 파라미터를 만들어 그 이벤트에 붙여야 한다. 둘 중 하나만 하면 이벤트가 버려진다.

### UI
- 메뉴: `MenuPager`, `TitlePage`, `MainMenuPage`, `EquipSelectPage`(고르기), `ItemCard`(카드), `SettingsPage`, `ConfirmDialog`, `ModeRecordPopup`, `ScreenFader`, `HoverScale`, `HoverLabel`.
- **고르기 카드는 씬에 미리 놓여 있다.** 아이템을 추가하면 카드를 복제해 같은 순서에 넣고 `EquipSelectPage` 배열에도 넣는다.
- 게임 중: `AchievementToast`(좌측 하단), `UnlockSummaryPanel`(판 끝 해금 창, 저주 해금도 여기), `StatePanel`(결과 창), `StageResultView`(결과 기록·건 저주 줄), `HealthView`, `AbilityGaugeView`, ESC 메뉴(왼쪽 슬라이드, 다시하기·메인메뉴로·타이틀로).
- 소리: `Core/AudioManager.cs`. 소리 없는 버튼에는 `ButtonClickSound`를 붙인다.

### 스토리 연출
- 오프닝 `Stage/IntroSequence.cs` + `IntroData`.
- 보스 대화 `BossDialoguePlayer` + `BossDialogueData`(줄마다 시작·끝 연출, BGM 칸). 등장 대화에서도 고블린 줄은 고블린 머리 위에 뜬다.
- 엔딩 `OutroSequence` + `OutroData`(밤 곡 칸 있음).

### 개발자 패널 (`UI/DevPanel.cs`, Game 씬)
- 무기 교체·수치 조정, 보스 바로가기(Final Boss, King Groggy/Defeat), Replay Intro, **Unlock All**(저주 포함), Reset Achv, Log Save, Reset Save, Fill Ability, Kill All, 배속.
- 패널을 쓴 판도 업적을 센다.

---

## 5. 콘텐츠 현황 (숫자는 design.md 표가 기준)

- 스토리: 웨이브 4개(각 100마리) + 보스 4전(화염 마법사 → 드릴 광부 → 야수 → 각성한 왕) → 엔딩.
- 무한 모드: 절차적 웨이브 + 보스 로테이션(바퀴마다 강해짐). 저주 9종 + 왕관(???). 찐엔딩.
- 적: 전사, 궁수, 저격수, 사마귀, 방패병, 돌격병, 골렘.
- 업적 30개 / 무기 4 / 스킬 3 / 강화 6 / 아티팩트 10 / 저주 10.
- 스티커 그림은 30개 모두 있다(`05. Art/Stickers/`, 실루엣 포함). 도감은 2면(1면 해금 업적 20, 2면 수집 업적 10).

---

## 6. 남은 일

- [ ] **웹 빌드 → itch.io 배포**(0번 1순위), 최종 용량 다시 재기
- [ ] 스트리핑을 Minimal보다 올릴지(Low/High): 올리면 작아지지만 리플렉션을 쓰는 Analytics가 깨지지 않는지 테스트 필요
- [ ] 남은 효과음: 덫 닫힘·부서짐, 운석, 낙뢰, 먼지바람, 저주 해금·장착, 최종 보스 전용
- [ ] UI 디자인: 버튼은 테두리·그림자를 더한 9-슬라이스로 정리됨. 창 틀(동의 창·결과 창 등)은 아직 단색
- [ ] 맵 아트(왕궁 미로 정원 컨셉, design.md "배경(맵)")
- [ ] 화염 마법사 원본 그림 외곽선이 얇다 → 튀면 1픽셀 외곽선 추가
- [ ] 회사 이름 `DefaultCompany`(바꾸면 Windows 저장 위치가 바뀌어 기존 세이브가 안 보임)
- [ ] README·포트폴리오 정리(AI 활용 = `Docs/devlog.md`)

---

## 7. 새 컴퓨터 준비

1. 저장소 pull/clone 후 Unity **6000.5.6f1**로 연다(2D URP, Input System, Cinemachine 3, DOTween, Unity Analytics 6.3.0).
2. Unity MCP(MCP for Unity) 연결을 확인한다. Claude Code에서 `mcp__UnityMCP__*` 도구가 보여야 한다.
3. 에디터에 같은 Unity 계정으로 로그인한다. Edit → Project Settings → Services에서 프로젝트 이름(`Explode_It`)이 보이면 Unity Cloud 연결 OK.
4. 저장 데이터는 PlayerPrefs라 **컴퓨터마다 따로**다. 해금이 비어 있으니 개발자 패널 `Unlock All`로 시작하면 편하다. 새 컴퓨터에서는 타이틀에서 플레이 기록 동의 창이 한 번 뜬다.
5. 새 세션에 위 첫 메시지 예시를 보내고, 2번 규칙을 메모리에 저장하게 한다.
