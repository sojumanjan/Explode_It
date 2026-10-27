-- Explode It: Unity Analytics SQL Data Explorer 쿼리 모음
-- 대시보드 SQL Data Explorer에 붙여 넣고, 각 제목을 리포트 이름으로 저장한다.
-- 모든 쿼리는 배포일(2026-10-27) 이후만 본다. 배포 전 개발자 테스트 기록을 빼기 위해서다.
-- 값 이름: mode = story / endless / true_ending, 판 result = clear / death / abandoned, 보스 result = defeated / died
-- kills는 그 판에서 실제로 잡은 수다. 스토리를 중간부터 시작한 판도 건너뛴 웨이브 몫은 들어가지 않는다(이벤트에 중간 시작 표시는 없다).


-- [1] 판_결과_요약: 모드별 판 수, 플레이어 수, 클리어율, 평균 생존 시간·처치 수
SELECT
    EVENT_JSON:mode::STRING AS mode,
    COUNT(*) AS runs,
    COUNT(DISTINCT USER_ID) AS players,
    ROUND(AVG(IFF(EVENT_JSON:result::STRING = 'clear', 1, 0)) * 100, 1) AS clear_pct,
    ROUND(AVG(IFF(EVENT_JSON:result::STRING = 'abandoned', 1, 0)) * 100, 1) AS abandoned_pct,
    ROUND(AVG(EVENT_JSON:survival_sec::INT)) AS avg_sec,
    ROUND(AVG(EVENT_JSON:kills::INT)) AS avg_kills
FROM EVENTS
WHERE EVENT_NAME = 'run_ended'
  AND EVENT_DATE >= '2026-10-27'
GROUP BY 1
ORDER BY runs DESC;


-- [2] 스토리_사망_구간: 진행 순서대로 어느 구간에서 판이 끝나는지. deaths = 적에게 죽음, quits = 일시정지 메뉴로 나감(사망으로 기록됨)
-- 최종 보스전부터 시작한 판은 웨이브 없이 보스만 만나 wave_reached = 0이므로, 보스를 먼저 본다.
-- 라벨 앞 번호는 차트가 이름순으로 정렬해도 진행 순서가 유지되게 붙인다.
WITH d AS (
    SELECT
        EVENT_JSON:wave_reached::INT AS w,
        EVENT_JSON:boss_reached::INT AS b,
        NULLIF(EVENT_JSON:killer::STRING, '') AS killer,
        USER_ID
    FROM EVENTS
    WHERE EVENT_NAME = 'run_ended'
      AND EVENT_JSON:mode::STRING = 'story'
      AND EVENT_JSON:result::STRING = 'death'
      AND EVENT_DATE >= '2026-10-27'
)
SELECT
    CASE
        WHEN b > 0 AND b >= w THEN (b * 2) || '. 보스 ' || b
        WHEN w = 0 THEN '0. 시작 전'
        ELSE (w * 2 - 1) || '. 웨이브 ' || w
    END AS place,
    COUNT_IF(killer IS NOT NULL) AS deaths,
    COUNT_IF(killer IS NULL) AS quits,
    COUNT(DISTINCT USER_ID) AS players
FROM d
GROUP BY 1
ORDER BY MIN(CASE WHEN b > 0 AND b >= w THEN b * 2 WHEN w = 0 THEN 0 ELSE w * 2 - 1 END);


-- [3] 보스별_난이도: 보스마다 처치율, 평균 전투 시간, 평균 피격 수
SELECT
    EVENT_JSON:boss::STRING AS boss,
    COUNT(*) AS fights,
    ROUND(AVG(IFF(EVENT_JSON:result::STRING = 'defeated', 1, 0)) * 100, 1) AS win_pct,
    ROUND(AVG(EVENT_JSON:fight_sec::INT)) AS avg_fight_sec,
    ROUND(AVG(EVENT_JSON:hits_taken::INT), 2) AS avg_hits
FROM EVENTS
WHERE EVENT_NAME = 'boss_ended'
  AND EVENT_DATE >= '2026-10-27'
GROUP BY 1
ORDER BY fights DESC;


-- [4] 플레이어별_판수_분포: 몇 판 하고 그만두는지(1판만 하고 떠난 비율), 그중 스토리를 깬 사람 수
WITH per_user AS (
    SELECT
        USER_ID,
        COUNT(*) AS runs,
        MAX(IFF(EVENT_JSON:mode::STRING = 'story' AND EVENT_JSON:result::STRING = 'clear', 1, 0)) AS cleared
    FROM EVENTS
    WHERE EVENT_NAME = 'run_ended'
      AND EVENT_DATE >= '2026-10-27'
    GROUP BY 1
)
SELECT
    IFF(runs >= 10, '10+', TO_VARCHAR(runs)) AS runs_bucket,
    COUNT(*) AS players,
    SUM(cleared) AS story_cleared_players
FROM per_user
GROUP BY 1
ORDER BY MIN(runs);


-- [5] 무기별_성적: 무기마다 판 수, 평균 처치 수, 스토리 클리어율
SELECT
    EVENT_JSON:weapon::STRING AS weapon,
    COUNT(*) AS runs,
    COUNT(DISTINCT USER_ID) AS players,
    ROUND(AVG(EVENT_JSON:kills::INT)) AS avg_kills,
    ROUND(AVG(IFF(EVENT_JSON:mode::STRING = 'story' AND EVENT_JSON:result::STRING = 'clear', 1, 0)) * 100, 1) AS story_clear_pct
FROM EVENTS
WHERE EVENT_NAME = 'run_ended'
  AND EVENT_DATE >= '2026-10-27'
GROUP BY 1
ORDER BY runs DESC;


-- [6] (선택) 튜토리얼_단계별_도달: 첫 판 기초 과제에서 어디까지 따라오는지
SELECT
    EVENT_JSON:step_name::STRING AS step,
    COUNT(DISTINCT USER_ID) AS players,
    ROUND(MEDIAN(EVENT_JSON:elapsed_sec::INT)) AS median_sec
FROM EVENTS
WHERE EVENT_NAME = 'tutorial_step'
  AND EVENT_DATE >= '2026-10-27'
GROUP BY 1
ORDER BY players DESC;


-- [7] 스킬별_처치: 판 시작 때 낀 스킬마다 판당 평균 스킬 처치 수와 발동 1회당 처치 수
-- 총합으로 보면 많이 고른 스킬(기본 블랙홀)이 무조건 커지므로 판당 평균과 발동당 효율로 본다.
-- 장착은 판 시작(run_started), 성적은 같은 판의 끝(run_ended)에서 가져와 run_id로 잇는다.
SELECT
    s.EVENT_JSON:ability::STRING AS ability,
    COUNT(*) AS runs,
    ROUND(AVG(e.EVENT_JSON:ability_kills::INT)) AS avg_ability_kills,
    ROUND(SUM(e.EVENT_JSON:ability_kills::INT) / NULLIF(SUM(e.EVENT_JSON:ability_uses::INT), 0), 1) AS kills_per_use
FROM EVENTS s
JOIN EVENTS e
  ON e.EVENT_NAME = 'run_ended'
 AND e.EVENT_JSON:run_id::STRING = s.EVENT_JSON:run_id::STRING
WHERE s.EVENT_NAME = 'run_started'
  AND s.EVENT_DATE >= '2026-10-27'
  AND e.EVENT_DATE >= '2026-10-27'
GROUP BY 1
ORDER BY runs DESC;


-- [8] 아티팩트_인기(클리어 유저): 스토리를 깬 사람의 판에서 아티팩트마다 낀 비율
-- 이벤트에 해금 목록이 없어, 안 낀 것이 싫어서인지 못 열어서인지 구분할 수 없다. 그래서 해금을 꽤 했을 클리어 유저만 보고, 성능이 아니라 인기로만 읽는다.
-- equip_pct는 두 칸을 합쳐 세므로 모두 더하면 100%를 넘는다. 참/거짓 값은 0/1 숫자로 저장되므로 ::INT로 비교한다.
WITH s AS (
    SELECT
        r.EVENT_JSON:artifact_1::STRING AS a1,
        r.EVENT_JSON:artifact_2::STRING AS a2,
        r.USER_ID
    FROM EVENTS r
    JOIN EVENTS e
      ON e.EVENT_NAME = 'run_ended'
     AND e.EVENT_JSON:run_id::STRING = r.EVENT_JSON:run_id::STRING
     AND e.EVENT_JSON:story_cleared::INT = 1
    WHERE r.EVENT_NAME = 'run_started'
      AND r.EVENT_DATE >= '2026-10-27'
),
slots AS (
    SELECT USER_ID, a1 AS artifact FROM s
    UNION ALL
    SELECT USER_ID, a2 AS artifact FROM s
)
SELECT
    COALESCE(NULLIF(artifact, ''), '(빈 칸)') AS artifact,
    COUNT(*) AS runs,
    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM s), 1) AS equip_pct,
    COUNT(DISTINCT USER_ID) AS players
FROM slots
GROUP BY 1
ORDER BY runs DESC;
