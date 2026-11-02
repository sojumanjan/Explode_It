import { burst, fly, flyV, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down bomb pop for Explode It: a warm core that shrinks away (no white flash),
// radial sparks that streak and drag to a stop, and a scorch that cools. Short, so it never hides the next telegraph.
export const meta = {
  name: "Bomb pop (flash)",
  desc: "Top-down bomb blast: a warm core that shrinks away, radial sparks dragging to a stop and a quick scorch, all gone in 0.4 s.",
  tags: ["explosion", "bomb", "top-down", "impact", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.4,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#4a1712", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#fff3a6", "#ffffff"], radius: 38, seed: 3 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), R = o.radius;
  // scorch on the floor: a soft translucent smudge that cools and thins (never a dithered checkerboard)
  // kept faint and short: on the game's bright floor a darker, longer one read as a shadow left behind (user call)
  const sc = 1 - seg(t, 0.06, 0.18);
  if (sc > 0) p.disc(0, 0, R * 0.42, C[0], 0.2 * sc);

  // hero frame: a warm core that shrinks away. No white disc and no wide white glow: on every bomb, many times a
  // second, a big white pop strained the eyes (user call). White stays only in a pinpoint at the centre.
  const hk = seg(t, 0, 0.1);
  if (hk < 1) {
    const r = R * 0.3 * (1 - ease.inQuad(hk)) + 0.5;
    p.circle(0, 0, r + 1, C[3]);
    p.circle(0, 0, r, C[4]);
    if (hk < 0.4) p.circle(0, 0, Math.max(1, r * 0.3), WHITE);
  }

  // No shock ring: in play the hollow ring sharpening at the edge read oddly against the range circle (user call).

  // radial sparks: two-tone streaks pointing along their motion, dragging to a stop
  burst(18, o.seed, (r) => {
    const a = r.range(0, Math.PI * 2), v = r.range(260, 520), life = r.range(0.16, 0.3), d0 = r.range(0, 0.03);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, vx = Math.cos(a) * v, vy = Math.sin(a) * v;
    const [x, y] = fly(0, 0, vx, vy, tt, 0, 9), [ux, uy] = flyV(vx, vy, tt, 0, 9);
    if (Math.hypot(x, y) > R + 6) return;
    const n = Math.hypot(ux, uy) || 1, L = Math.min(9, n * 0.025) + 1;
    p.line(x, y, x - (ux / n) * L, y - (uy / n) * L, (u) => (u < 0.35 ? (k < 0.3 ? C[5] : C[4]) : C[3]));
    p.light(x, y, C[4], 0.6 * (1 - k));
  });

  const pop = Math.exp(-t / 0.06);
  p.glow(0, 0, R * 0.6, C[3], 0.25 * pop);
}
