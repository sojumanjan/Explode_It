import { burst, fly, flyV, seg, ease, fit, WHITE, TAU } from "../../core/index.js";

// A top-down black hole for Explode It: for `pull` seconds streaks spiral in from the rim and a dark core swells,
// then it collapses to a white point, beats once and pops in a filled flash. The game's pull time grows with an
// upgrade (+1 s), so render one sheet per pull length (-Opts '{"pull":3}').
export const meta = {
  name: "Black hole implode",
  desc: "Top-down black hole: streaks spiral in from the rim while a dark core swells, then it collapses to a white point and pops in a filled flash and sparks.",
  tags: ["arcane", "black hole", "implosion", "top-down", "skill", "one-shot"],
  size: [176, 176],
  origin: [88, 88],
  duration: 2.4,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#140a24", "#3b1a6b", "#6b33b8", "#a46bff", "#d9b8ff", "#ffffff"], radius: 76, pull: 2, seed: 9 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6), R = o.radius, P = o.pull, end = P + 0.4;
  if (t >= end) return;

  // the pull: streaks born all through the pull, spiralling in faster as they near the core
  // the pop lands exactly at t = pull, the game's explosion moment, so the collapse takes the last 0.1 s of the pull
  if (t < P - 0.1) {
    burst(130, o.seed, (r, i) => {
      const born = (i / 130) * (P - 0.3) + r.range(0, 0.15), life = r.range(0.35, 0.55), tt = t - born;
      if (tt < 0 || tt > life) return;
      const q = ease.inQuad(tt / life), a0 = r.range(0, TAU), rad = R * r.range(0.85, 1.05) * (1 - q) + 6 * q;
      const a = a0 + q * 1.4, x = Math.cos(a) * rad, y = Math.sin(a) * rad;
      // tail points back out along the spiral
      const L = 5 + 9 * q, ta = a - 0.25;
      p.line(x, y, Math.cos(ta) * (rad + L), Math.sin(ta) * (rad + L), (u) => (u < 0.4 ? (q > 0.6 ? C[4] : C[3]) : C[1]));
      if (q > 0.5) p.light(x, y, C[3], 0.3 * q);
    });
    // the core: a dark disc with a violet rim that swells and throbs
    const g = seg(t, 0, P), rr = 5 + 6 * ease.outQuad(g) + Math.sin(t * 18) * 0.8 * g;
    p.circle(0, 0, rr + 2, C[3]);
    p.circle(0, 0, rr + 1, C[2]);
    p.circle(0, 0, rr, C[0]);
    p.glow(0, 0, rr * 3, C[2], 0.25 + 0.2 * g);
  }

  // collapse: the core crushes to a white point and holds a beat
  if (t >= P - 0.1 && t < P) {
    const k = seg(t, P - 0.1, P - 0.04), rr = 11 * (1 - ease.inQuad(k)) + 1;
    p.circle(0, 0, rr, k < 0.6 ? C[0] : WHITE);
    p.ring(0, 0, rr + 1, 1, C[4]);
  }

  // the pop: a filled flash that blooms and shrinks back, radial sparks. No hollow ring: on the bomb blast a ring
  // sharpening at the range edge read oddly (user call), so the black hole follows the same rule.
  const pt = t - P;
  if (pt >= 0) {
    const fk = seg(pt, 0, 0.16);
    if (fk < 1) {
      const r = R * (fk < 0.25 ? 0.3 + 0.25 * ease.outQuad(fk / 0.25) : 0.55 * (1 - ease.inQuad((fk - 0.25) / 0.75)));
      p.circle(0, 0, r + 2, C[3]);
      p.circle(0, 0, r, fk < 0.3 ? WHITE : C[4]);
      if (fk >= 0.3) p.circle(0, 0, r * 0.6, WHITE);
    }
    burst(20, o.seed + 5, (r) => {
      const a = r.range(0, TAU), v = r.range(260, 480), life = r.range(0.18, 0.32);
      if (pt > life) return;
      const vx = Math.cos(a) * v, vy = Math.sin(a) * v, [x, y] = fly(0, 0, vx, vy, pt, 0, 8), [ux, uy] = flyV(vx, vy, pt, 0, 8);
      if (Math.hypot(x, y) > R + 4) return;
      const n = Math.hypot(ux, uy) || 1, L = Math.min(8, n * 0.02) + 1;
      p.line(x, y, x - (ux / n) * L, y - (uy / n) * L, (u) => (u < 0.4 ? WHITE : C[3]));
      p.light(x, y, C[3], 0.5 * (1 - pt / life));
    });
    p.glow(0, 0, R, C[3], 0.45 * Math.exp(-pt / 0.08));
  }
}
