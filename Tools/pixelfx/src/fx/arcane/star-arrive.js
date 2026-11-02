import { burst, fly, seg, ease, fit, WHITE, TAU } from "../../core/index.js";

// A top-down arrival flash for Explode It: the awakened king lands on a star tip during the star rite. A four-point
// violet and gold glint flares and shrinks, motes spray out and twinkle. Small and filled (no ring), so the tip and the
// next star stroke stay readable.
export const meta = {
  name: "Star arrive",
  desc: "Top-down arrival flash: a four-point violet and gold glint flares and shrinks while motes spray out and twinkle, gone in 0.4 s.",
  tags: ["arcane", "teleport", "star", "top-down", "boss", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.4,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a1446", "#5a2a9a", "#a46bff", "#d9b8ff", "#ffe58a"], seed: 11 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 5);

  // the glint: long vertical and horizontal spikes, short diagonals, collapsing to a point
  const k = seg(t, 0, 0.22);
  if (k < 1) {
    const s = 1 - ease.inQuad(k), L = 22 * s + 1, l = 9 * s + 1;
    p.line(0, -L, 0, L, (u) => (Math.abs(u - 0.5) < 0.15 ? WHITE : C[3]));
    p.line(-L, 0, L, 0, (u) => (Math.abs(u - 0.5) < 0.15 ? WHITE : C[3]));
    p.line(-l, -l, l, l, C[4]);
    p.line(-l, l, l, -l, C[4]);
    p.circle(0, 0, 3 * s + 1, k < 0.3 ? WHITE : C[4]);
    p.glow(0, 0, 26, C[2], 0.45 * s);
  }

  // motes: violet and gold specks thrown out, twinkling as they slow
  burst(14, o.seed, (r, i) => {
    const a = (i / 14) * TAU + r.range(-0.2, 0.2), v = r.range(90, 180), life = r.range(0.22, 0.36);
    if (t > life) return;
    const q = t / life, [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, t, 0, 7);
    if (Math.sin(t * 60 + i) > -0.3) p.px(x, y, i % 3 === 0 ? C[4] : q < 0.5 ? C[3] : C[2]);
    p.light(x, y, C[2], 0.35 * (1 - q));
  });
}
