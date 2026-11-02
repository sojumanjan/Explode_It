import { burst, fly, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down claw rake for Explode It: where the beast's charge ends, three parallel claw streaks rip across the
// path (facing right = the charge direction), flash white, cool to red and fade, with a few chips thrown forward.
// `width` matches the charge box width in pixels so the marks span exactly what was hit.
export const meta = {
  name: "Claw rake",
  desc: "Top-down claw finish: three parallel streaks rip across the charge end, flash white, cool to red and fade, chips thrown forward, gone in 0.35 s.",
  tags: ["melee", "claw", "slash", "top-down", "boss", "directional", "one-shot"],
  size: [168, 168],
  origin: [84, 84],
  duration: 0.36,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a0d12", "#8c1a20", "#d83a2e", "#ff8a5a", "#ffe0c8", "#ffffff"], width: 120, seed: 8 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6), W = o.width / 2;
  // three streaks across the path, raked forward-down at a slant, drawn one after another in quick succession
  for (let i = 0; i < 3; i++) {
    const start = i * 0.025, k = seg(t, start, start + 0.07), fade = 1 - seg(t, start + 0.12, start + 0.3);
    if (k <= 0 || fade <= 0) continue;
    // spacing and slant grow with the width so a wide charge gets a wide paw, not three hairlines
    const off = (i - 1) * W * 0.22, lean = W * 0.18, x0 = off + lean, y0 = -W, x1 = off - lean, y1 = W;
    const ex = x0 + (x1 - x0) * ease.outQuart(k), ey = y0 + (y1 - y0) * ease.outQuart(k);
    const col = t - start < 0.08 ? WHITE : fade > 0.6 ? C[4] : fade > 0.3 ? C[3] : C[2];
    p.line(x0, y0, ex, ey, col, fade);
    p.line(x0 + 1, y0 + 1, ex + 1, ey, C[2], fade * 0.8);
    p.line(x0 - 1, y0, ex - 1, ey, col, fade * 0.7);
    p.glowLine(x0, y0, ex, ey, 2, C[3], 0.35 * fade);
  }

  // chips thrown on in the charge direction
  burst(8, o.seed, (r) => {
    const a = r.range(-0.6, 0.6), v = r.range(140, 260), life = r.range(0.14, 0.26), d0 = r.range(0.02, 0.06);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const [x, y] = fly(0, r.range(-W * 0.8, W * 0.8), Math.cos(a) * v, Math.sin(a) * v, tt, 0, 9);
    p.px(x, y, tt < life * 0.5 ? WHITE : C[3]);
    p.light(x, y, C[3], 0.4 * (1 - tt / life));
  });
}
