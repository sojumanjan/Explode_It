# 작업 인수인계 (2026-10-23 기준)

다른 컴퓨터·새 Claude Code 세션에서 바로 이어서 작업하기 위한 요약이다. 새 세션을 시작하면 이 파일 → `CLAUDE.md` → `Docs/design.md` 순서로 읽힌다.

> 새 세션 첫 메시지 예: "Docs/handoff.md, CLAUDE.md, Docs/design.md 읽고 이어서 하자. handoff 2번의 작업 규칙을 메모리에 저장해 줘."

---

## 0. 오늘(10/23)까지 한 것과 다음에 할 것

### 다음에 바로 할 것 (우선순위 순)
1. **플레이 기록(Unity Analytics)이 들어왔는지 확인** ← 1순위
   - 10/23 에디터에서 동의 → 한 판 플레이로 전송까지 해 봤다. 대시보드 반영이 늦어 확인 못 하고 넘어왔다.
   - 확인할 곳: cloud.unity.com → 프로젝트 `Explode_It`(ID `23b20e6a-10a6-445c-b1c3-5682b9220cbd`) → Analytics → 이벤트 확인 화면(Event Browser / 실시간 이벤트). 그래프(Data Explorer)는 몇 시간 늦게 뜬다.
   - 볼 것: `run_started` / `run_ended` / `player_hit` / `boss_started` / `boss_ended`가 들어왔는지, **잘못된 이벤트(Invalid)**로 버려진 게 있는지. 있으면 파라미터 이름·타입이 대시보드와 코드에서 어긋난 것이다(4번 "플레이 기록" 표와 대조).
   - 확인이 끝나면 `02. Data/AnalyticsData.asset`의 **Send From Editor를 끈다**. 지금 켜져 있다. 켜 두면 개발 중 플레이가 실제 기록에 섞인다.
   - 그다음 개발 빌드 → itch.io 웹 빌드에서도 들어오는지 확인한다. WebGL 지원은 문서상 OK이고, 실측은 아직이다.
2. **크레딧 4장면(화해) 손보기**: 1~3장면(법사·광부·야수)은 사용자가 보고 다듬었다. 4장면은 초안 그대로다. 다 다듬은 뒤 장면 길이 합(장면 사이 페이드 0.8초씩, 첫 장면 전 4초 포함)이 크레딧 길이(약 50초)와 맞는지 본다.
3. **크레딧 마무리**
   - 크레딧 곡: 지금 `Bgm_Main` 임시
   - "도움 주신 분들": 지금 "(채워 넣기)", 사용자가 채운다
   - 마지막 낮잠 그림을 넣을지: 사용자 미정
4. 아래 6번 "남은 일"

### 10/23에 끝낸 것
- **모드 기록 창** (`UI/ModeRecordPopup.cs`, `RecordRow.cs`)
  - 메인에서 스토리·무한을 누르면 기록 창이 뜬다. 최단 클리어 / 최다 처치 / 최장 생존을 아이콘과 함께 보여 주고, 클리어했으면 CLEAR 도장, 무한은 장착한 저주 아이콘 줄. 창의 시작 버튼으로 판 시작.
  - 첫 판은 창 없이 바로 시작한다.
  - 무한 기록은 하나로 합쳤다(저주 9개 기록 삭제).
- 사망 결과 창에 이번 판에 건 저주 아이콘 줄. 첫 판 시작 페이드 1.5초.
- 엔딩 "그날 밤…" 밤 곡(`Bgm_Night`), 최종 보스 처치 대화 개그 왕 곡(`Bgm_KingGag`).
- 블랙홀 폭탄 몸통 = 스킬 그림(1.3배, 회전 없음).
- **찐엔딩** (`Stage/TrueEndingData`, `TrueEndingSequence`, `StageRunner` 분기)
  - 흐름: 저주 9개 + 왕관(???)으로 무한 모드 → 웨이브 없이 결전 대화(`Dialogue_True_King`) → 각성 왕 → 처치하면 대화 없이 깊게 확대하며 까맣게 → 크레딧(오른쪽) + 왼쪽 장면 → 마지막 문구 → 업적 "진짜 끝"(`TrueEndingCleared`) → 결과 창.
  - 찐엔딩 판은 기록을 남기지 않는다.
  - 크레딧은 꾹 누르면 3배속(왼쪽 장면도 같이 빨라짐), 본 적 있으면 스페이스로 건너뛰기, 크레딧 동안 Esc 일시정지는 막았다.
- **크레딧 왼쪽 장면** (`Stage/CreditsShot*`)
  - 구성: 씬의 `CreditsShots/Shot1~4`(장소·배우·숨길 것) + 대본 SO `02. Data/Story/CreditsShot_*.asset`.
  - 대본 한 줄 = 시각, 배우 번호, 그림 바꾸기, 동작. 동작은 뽀잉 / 떨기 / 콩콩(이동을 주면 걷기) / 옮기기 / 뒤돌기 / 사라지기 / 나타나기.
  - 장면: 1 법사(왕 머리 위 돈 아이콘), 2 광부(사용자가 만든 x≈-69 분수대 광장, 땅속을 들락날락하며 구덩이), 3 야수(지그재그 도망), 4 화해(초안).
- **그림**
  - 화염 마법사 그림을 원본으로 교체(소매 안쪽 채움, 원본 백업 `ArtSource/Original/boss/`)
  - 동전 7종 `05. Art/Props/Coins/`
  - 구덩이·흙더미 `05. Art/boss/Miner/dig_*`
  - 기록 아이콘 3종 `05. Art/UI/Icon_Record_*`
- **플레이 기록 수집** (4번 "플레이 기록"): 첫 실행 동의 창, 설정 "기록 보내기", 이벤트 7종.

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
- `Stage/TrueEndingData`: 결전 대화, 처치 후 확대·페이드, 크레딧 줄 목록(`CreditsEntry`), 마지막 문구, 첫 장면 지연(4초).
- `Stage/TrueEndingSequence`: Game 씬 `TrueEnding` 오브젝트, UI는 `Canvas/Credits`(오른쪽 크레딧 + 그러데이션, 왼쪽 장면, 덮개).
- 왼쪽 장면은 `CreditsShot`(씬) + `CreditsShotData`(대본). 장면 추가 방법: `CreditsShots` 아래 장면 복제 → 배우·자리 바꾸기 → 대본 SO 새로 만들기 → `TrueEnding`의 장면 목록에 넣기.

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
- 업적 27개(10/23 "진짜 끝" 추가) / 무기 4 / 스킬 3 / 강화 6 / 아티팩트 10 / 저주 10.
- 스티커 그림은 일부만 있고, 없는 것은 공용 빈 스티커(`05. Art/UI/Sticker_Empty.png`)로 대신한다.

---

## 6. 남은 일

- [ ] **플레이 기록 확인**(0번 1순위) → Send From Editor 끄기 → 개발 빌드·웹 빌드에서 확인. 기록이 모이면 개인별 판 기록을 시간순으로 뽑는 SQL·내보내기 방법 정리
- [ ] **크레딧 4장면(화해) 다듬기**, 장면 길이 합 맞추기
- [ ] 크레딧 곡, "도움 주신 분들", 마지막 낮잠 그림 여부
- [ ] 크레딧에서 안 쓴 동전 그림 2장(무더기·큰 무더기)을 쓸지
- [ ] **인게임 고블린(Player3) 교체**: 컷씬 고블린과 맞춘 새 그림 프롬프트를 줬다(참조: 지금 Player3 + 2컷 책 읽는 고블린). 그림을 받으면 마젠타 제거 → 지금 Player3 크기에 맞춰 교체
- [ ] 새 옷 사망 그림(`player_die` / `고블린사망`)
- [ ] 화염 마법사 원본 그림이 다른 캐릭터보다 외곽선이 얇다 → 튀면 1픽셀 외곽선 추가
- [ ] **도감(스티커) 페이지**: 해금 업적을 도감에 넣기
- [ ] 업적 목록 확정 후 **스티커 시트 한 장으로 생성**(4×3 격자, 흰 테두리 스티커, 글자 없음). "왕국의 끝"·"진짜 끝" 등은 지금 공용 빈 스티커
- [ ] 남은 효과음: 덫 닫힘·부서짐, 운석, 낙뢰, 먼지바람, 저주 해금·장착, 최종 보스 전용, 편지 펼침(`OutroData`의 Letter Sound 비어 있음)
- [ ] 픽셀 로고 글꼴
- [ ] 맵 아트(왕궁 미로 정원 컨셉, design.md "배경(맵)")
- [ ] 해금 창을 보상마다 한 장 → 업적당 한 장으로 묶을지(사용자 답 대기)
- [ ] 무기·스킬 고르기에도 해금 조건 + CLEAR 도장을 보일지(지금은 아티팩트만)
- [ ] `Docs/schedule.md`(10/8 이후 그대로), `CLAUDE.md` "현재 단계" 갱신(무한 모드·저주·찐엔딩 끝남 반영)
- [ ] 4주차: 타격감, 사운드, 아트 교체, WebGL 빌드 → itch.io 배포
- 정리할 것: 안 쓰는 `07. Materials/SpriteSilhouette.shader`, `Mat_Silhouette*.mat`. 외부 에셋 팩은 `09. ThirdParty`로 옮기는 게 규칙상 맞다.

---

## 7. 새 컴퓨터 준비

1. 저장소 pull/clone 후 Unity **6000.5.6f1**로 연다(2D URP, Input System, Cinemachine 3, DOTween, Unity Analytics 6.3.0).
2. Unity MCP(MCP for Unity) 연결을 확인한다. Claude Code에서 `mcp__UnityMCP__*` 도구가 보여야 한다.
3. 에디터에 같은 Unity 계정으로 로그인한다. Edit → Project Settings → Services에서 프로젝트 이름(`Explode_It`)이 보이면 Unity Cloud 연결 OK.
4. 저장 데이터는 PlayerPrefs라 **컴퓨터마다 따로**다. 해금이 비어 있으니 개발자 패널 `Unlock All`로 시작하면 편하다. 새 컴퓨터에서는 타이틀에서 플레이 기록 동의 창이 한 번 뜬다.
5. 새 세션에 위 첫 메시지 예시를 보내고, 2번 규칙을 메모리에 저장하게 한다.
