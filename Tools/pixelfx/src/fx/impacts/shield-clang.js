import { burst, fly, flyV, seg, fit, WHITE } from "../../core/index.js";

// A top-down shield block for Explode It: a blast meets a raised shield. The shield face (a short line through the
// origin, facing right) flashes, a four-point glint rings out, and metal sparks glance back the way the blast came.
// Tells the player "this side is guarded" without hiding the shield bearer.
export const meta = {
  name: "Shield clang",
  desc: "Top-down shield block: the shield face flashes white, a four-point glint rings out and metal sparks glance back toward the blast, gone in 0.35 s.",
  tags: ["impact", "block", "shield", "metal", "top-down", "directional", "one-shot"],
  size: [80, 64],
  origin: [28, 32],
  duration: 0.36,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2b3550", "#5a6f9a", "#a9c4e8", "#ffe7a0", "#ffffff"], face: 11, seed: 6 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 5), F = o.face;
  // the shield face: a bright vertical edge that cools blue
  const k = seg(t, 0, 0.16);
  if (k < 1) {
    const c = k < 0.3 ? WHITE : k < 0.65 ? C[2] : C[1];
    p.line(0, -F, 0, F, c);
    p.line(1, -F + 2, 1, F - 2, c, 1 - k);
    p.glowLine(0, -F, 0, F, 3, C[2], 0.5 * (1 - k));
  }

  // the glint: a four-point star on the face, long along the face, short across it
  if (t < 0.1) {
    const s = 1 - t / 0.1, L = 12 * s + 2, l = 6 * s + 1;
    p.line(0, -L, 0, L, WHITE);
    p.line(-l, 0, l, 0, WHITE);
    p.px(0, 0, WHITE);
    p.glow(0, 0, 10, C[3], 0.5 * s);
  }

  // sparks glancing back toward the blast (to the right), dragging to a stop
  burst(11, o.seed, (r) => {
    const a = r.range(-1.0, 1.0), v = r.range(150, 320), life = r.range(0.14, 0.3), d0 = r.range(0, 0.03);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const y0 = r.range(-F * 0.6, F * 0.6), vx = Math.cos(a) * v, vy = Math.sin(a) * v;
    const [x, y] = fly(2, y0, vx, vy, tt, 0, 10), [ux, uy] = flyV(vx, vy, tt, 0, 10);
    const n = Math.hypot(ux, uy) || 1, L = Math.min(6, n * 0.02) + 1, q = tt / life;
    p.line(x, y, x - (ux / n) * L, y - (uy / n) * L, (u) => (u < 0.4 ? (q < 0.5 ? WHITE : C[3]) : C[2]));
    p.light(x, y, C[3], 0.5 * (1 - q));
  });
}
