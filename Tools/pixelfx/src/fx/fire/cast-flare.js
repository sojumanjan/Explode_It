import { burst, fly, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down cast flare for Explode It: the fire mage lets a fireball go. A short hot cone puffs out the way it flies
// (facing right), a white pip at the hand, and a few sparks trail after it. Tiny and quick so the fireball itself reads.
export const meta = {
  name: "Cast flare",
  desc: "Top-down cast flare: a short hot cone puffs out the way the shot flies, a white pip at the hand and a few trailing sparks, gone in 0.22 s.",
  tags: ["fire", "cast", "muzzle", "top-down", "directional", "one-shot"],
  size: [56, 40],
  origin: [14, 20],
  duration: 0.22,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#5a1408", "#c9281f", "#ff6a2a", "#ffb347", "#fff0a0", "#ffffff"], seed: 3 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6);
  const k = seg(t, 0, 0.14);
  if (k < 1) {
    // the cone: a few overlapping discs pushed forward, shrinking and cooling
    for (let i = 0; i < 4; i++) {
      const d = 3 + i * 4 * (0.6 + 0.4 * ease.outQuad(k)), r = (5 - i) * (1 - ease.inQuad(k)) + 0.5;
      p.circle(d, 0, r, i === 0 && k < 0.4 ? WHITE : k < 0.5 ? C[4 - Math.min(i, 2)] : C[3 - Math.min(i, 2)]);
    }
    p.glow(6, 0, 12, C[3], 0.5 * (1 - k));
  }

  burst(5, o.seed, (r) => {
    const a = r.range(-0.5, 0.5), v = r.range(120, 200), life = r.range(0.1, 0.18), d0 = r.range(0, 0.03);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const [x, y] = fly(2, 0, Math.cos(a) * v, Math.sin(a) * v, tt, 0, 10);
    p.px(x, y, tt < life * 0.5 ? C[5] : C[3]);
  });
}
