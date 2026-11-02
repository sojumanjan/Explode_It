import { burst, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down fireball wake for Explode It: one puff left behind a flying fireball. Dropped every few hundredths of a
// second along the path, the puffs read as a trail of afterimages: each starts as a hot ball the size of the fireball,
// shrinks, cools from yellow to red to soot and sheds a spark or two.
export const meta = {
  name: "Fireball wake",
  desc: "Top-down fireball afterimage: a hot puff the size of the fireball that shrinks, cools to red and soot and sheds a spark, gone in 0.3 s.",
  tags: ["fire", "trail", "projectile", "top-down", "one-shot"],
  size: [32, 32],
  origin: [16, 16],
  duration: 0.3,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a1418", "#8c1a14", "#d8361e", "#ff6a2a", "#ffb347", "#fff0a0"], radius: 7, seed: 2 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6), k = seg(t, 0, 0.26);
  if (k < 1) {
    const r = o.radius * (1 - ease.inQuad(k)) + 0.5;
    const col = k < 0.2 ? C[5] : k < 0.45 ? C[4] : k < 0.7 ? C[3] : k < 0.88 ? C[2] : C[0];
    p.circle(0, 0, r, col, k < 0.88 ? 1 : 0.6);
    if (k < 0.45) p.circle(-r * 0.25, -r * 0.3, r * 0.45, k < 0.2 ? WHITE : C[5]);
    p.glow(0, 0, r * 2.2, C[3], 0.3 * (1 - k));
  }

  burst(2, o.seed, (r) => {
    const a = r.range(0, Math.PI * 2), v = r.range(20, 45), life = r.range(0.15, 0.25), d0 = r.range(0.02, 0.05);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    p.px(Math.cos(a) * v * tt, Math.sin(a) * v * tt, tt < life * 0.5 ? C[5] : C[3]);
  });
}
