import { burst, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down gold wake for Explode It: one glint left behind the king's flying crown. Dropped every few hundredths of
// a second along its path, the glints trace the boomerang and the drop so the returning crown can be read and dodged.
// Each is a small four-point twinkle that shrinks with a spark or two falling off.
export const meta = {
  name: "Gold wake",
  desc: "Top-down gold afterimage: a small four-point twinkle that shrinks and cools with a spark or two falling off, gone in 0.35 s.",
  tags: ["arcane", "trail", "gold", "sparkle", "top-down", "one-shot"],
  size: [32, 32],
  origin: [16, 16],
  duration: 0.35,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#6a4a10", "#c99a2a", "#ffd25a", "#fff2b0"], seed: 3 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 4), k = seg(t, 0, 0.3);
  if (k < 1) {
    const s = 1 - ease.inQuad(k), L = 6 * s + 0.5;
    const c = k < 0.25 ? WHITE : k < 0.55 ? C[3] : C[2];
    p.line(0, -L, 0, L, c);
    p.line(-L, 0, L, 0, c);
    p.circle(0, 0, 1.5 * s + 0.5, k < 0.4 ? C[3] : C[1]);
    p.glow(0, 0, 8, C[2], 0.3 * s);
  }

  burst(2, o.seed, (r) => {
    const x0 = r.range(-3, 3), life = r.range(0.18, 0.3), d0 = r.range(0.03, 0.06), tt = t - d0;
    if (tt < 0 || tt > life) return;
    p.px(x0, tt * 30, tt < life * 0.5 ? C[3] : C[1]);
  });
}
