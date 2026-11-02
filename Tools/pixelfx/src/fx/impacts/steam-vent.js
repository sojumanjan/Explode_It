import { burst, fly, seg, ease, fit, WHITE, TAU } from "../../core/index.js";

// A top-down overheat vent for Explode It: the drill miner takes a hit and its engine bleeds heat. Soft steam puffs
// burst out round the body and drift up as they thin, a few hot sparks spit off and a short orange glow flares.
// The same sheet scaled up reads as the full overheat when the miner breaks down.
export const meta = {
  name: "Steam vent",
  desc: "Top-down overheat vent: soft steam puffs burst out and drift up as they thin, hot sparks spit off and an orange glow flares, gone in 0.6 s.",
  tags: ["impact", "steam", "heat", "machine", "top-down", "boss", "one-shot"],
  size: [112, 112],
  origin: [56, 64],
  duration: 0.6,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { steam: ["#8e8a94", "#c8c4cc", "#f2eff4"], palette: ["#8c1a14", "#ff6a2a", "#ffb347", "#fff0a0"], seed: 6 },
};

export function draw(p, t, o) {
  const S = fit(o.steam, 3), C = fit(o.palette, 4);

  // an orange flare at the engine for the first beat
  const fk = seg(t, 0, 0.16);
  if (fk < 1) {
    p.circle(0, -6, 6 * (1 - fk) + 1, fk < 0.4 ? C[3] : C[2]);
    p.glow(0, -6, 24, C[1], 0.45 * (1 - fk));
  }

  // steam: soft translucent puffs thrown out round the body that rise, swell and thin, lit on top
  burst(10, o.seed, (r, i) => {
    const a = (i / 10) * TAU + r.range(-0.3, 0.3), v = r.range(60, 120), life = r.range(0.35, 0.55), d0 = r.range(0, 0.06);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, [x, y0] = fly(0, -6, Math.cos(a) * v, Math.sin(a) * v * 0.6, tt, 0, 6);
    const y = y0 - 22 * ease.outQuad(k), rr = 4 + 7 * ease.outQuad(k), al = 0.8 * (1 - ease.inQuad(k));
    p.disc(x, y, rr, S[1], al);
    p.disc(x - rr * 0.2, y - rr * 0.3, rr * 0.6, S[2], al * 0.8);
  });

  // sparks spat out of the engine
  burst(7, o.seed + 2, (r) => {
    const a = r.range(0, TAU), v = r.range(140, 240), life = r.range(0.14, 0.26);
    if (t > life) return;
    const [x, y] = fly(0, -6, Math.cos(a) * v, Math.sin(a) * v, t, 0, 8);
    p.px(x, y, t < life * 0.5 ? WHITE : C[2]);
    p.light(x, y, C[1], 0.4 * (1 - t / life));
  });
}
