import { burst, fly, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down rift spark for Explode It: one stretch of the awakened king's heaven-splitting cut. Laid end to end along
// the cut line (facing right = along the line), they make the whole line rip open at once: a hot violet-white streak
// that thins from its middle out, with sparks flicking off both sides.
export const meta = {
  name: "Rift spark",
  desc: "Top-down rift stretch: a violet-white streak along the cut that thins from the middle out while sparks flick off both sides, gone in 0.35 s.",
  tags: ["arcane", "slash", "rift", "top-down", "boss", "directional", "one-shot"],
  size: [48, 32],
  origin: [24, 16],
  duration: 0.35,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a1446", "#5a2a9a", "#a46bff", "#d9b8ff"], length: 22, seed: 13 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 4), H = o.length / 2, k = seg(t, 0, 0.26);
  if (k < 1) {
    // overlapping a little with the next stretch so the line reads unbroken
    const half = H * (1 - ease.inQuad(k)) + 0.5, c = k < 0.25 ? WHITE : k < 0.6 ? C[3] : C[2];
    p.line(-half, 0, half, 0, c);
    if (k < 0.5) {
      p.line(-half * 0.8, -1, half * 0.8, -1, C[2]);
      p.line(-half * 0.8, 1, half * 0.8, 1, C[2]);
    }
    p.glowLine(-half, 0, half, 0, 3, C[2], 0.4 * (1 - k));
  }

  burst(4, o.seed, (r, i) => {
    const side = i % 2 ? 1 : -1, a = side * (Math.PI / 2) + r.range(-0.6, 0.6), v = r.range(60, 120), life = r.range(0.14, 0.24);
    if (t > life) return;
    const [x, y] = fly(r.range(-H, H), 0, Math.cos(a) * v, Math.sin(a) * v, t, 0, 8);
    p.px(x, y, t < life * 0.5 ? C[3] : C[2]);
  });
}
