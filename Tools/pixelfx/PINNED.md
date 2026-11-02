# pixelfx (고정 버전)

- 원본: https://github.com/patch-gg/pixelfx
- 버전: 0.3.0, 커밋 a84159688af3ca37a4be05a0d422b94451b2e24e
- 뺀 것: `gallery/`(Patch Pixel 폰트 포함, MIT 대상 아님), `docs/readme/`(영상과 이미지), 갤러리용 도구 `tools/build-gallery.mjs`, `tools/serve.mjs`
- 렌더 결과 `records/`, `sheets/`는 커밋하지 않는다(.gitignore). 게임에 쓸 시트는 `Tools/pixelfx-export.ps1`로 `Assets/_Project/05. Art/PixelFx/`에 복사한다.

버전을 올릴 때는 같은 방식으로 새 커밋을 받아 덮어쓰고 이 파일의 커밋 해시를 바꾼다.

## 이 프로젝트에서 더한 것

- `src/fx/explosions/bomb-pop-a.js`, `-b.js`, `-c.js`: Explode It용 탑다운 폭탄 폭발(0.36~0.5초)
- `src/fx/melee/topdown-slash.js`(적 베기, 100도), `src/fx/impacts/shield-clang.js`(방패 막힘), `src/fx/arcane/blackhole-implode.js`(블랙홀)
- 야수: `spawn/fire-soul-pop.js`, `impacts/dash-kick.js`, `melee/claw-rake.js` / 법사: `fire/cast-flare.js`, `fire/fireball-wake.js`, `fire/field-swirl.js`
- 광부: `impacts/steam-vent.js`, `explosions/dirt-burst.js` (다이너마이트는 bomb-pop-b 색 바꿈)
- 원본 라이브러리에는 없다. 버전을 올릴 때 이 파일들은 옮겨 둔다.
