import { burst, fly, seg, ease, fit, WHITE, TAU } from "../../core/index.js";

// A top-down "fire soul" pop for Explode It: the beast boss's red will-o'-wisp is hit. A white heart flashes, flame
// tongues burst outward and shrink as they cool, embers drift on and a thin puff of soot thins away. No ring: rings
// sharpening at the edge read oddly in this game (user call on the bomb blast).
export const meta = {
  name: "Fire soul pop",
  desc: "Top-down wisp burst: a white heart flashes, red flame tongues burst outward and shrink as they cool, embers drift and a thin soot puff fades, gone in 0.5 s.",
  tags: ["spawn", "death", "fire", "wisp", "top-down", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.5,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a0d0d", "#8c1a14", "#d8361e", "#ff6a2a", "#ffb347", "#fff0a0", "#ffffff"], radius: 30, seed: 7 },
};

const SOOT = 0x2a1a1e;

export function draw(p, t, o) {
  const C = fit(o.palette, 7), R = o.radius;

  // soot: a soft translucent puff that swells and thins behind everything
  const sk = seg(t, 0.06, 0.45);
  if (sk > 0 && sk < 1) p.disc(0, 0, R * (0.35 + 0.35 * ease.outQuad(sk)), SOOT, 0.35 * (1 - sk));

  // the heart: a white core in a hot rim that collapses fast
  const hk = seg(t, 0, 0.12);
  if (hk < 1) {
    const r = R * 0.4 * (1 - ease.inQuad(hk)) + 1;
    p.circle(0, 0, r + 2, C[4]);
    p.circle(0, 0, r, hk < 0.4 ? WHITE : C[5]);
    p.glow(0, 0, R, C[3], 0.5 * (1 - hk));
  }

  // flame tongues: teardrops thrown outward, fat head first, cooling down the ramp as they shrink
  burst(11, o.seed, (r, i) => {
    const a = (i / 11) * TAU + r.range(-0.25, 0.25), v = r.range(150, 260), life = r.range(0.22, 0.36), d0 = r.range(0, 0.03);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, tt, 0, 9);
    const size = 4.5 * (1 - ease.inQuad(k)) + 0.5, heat = 1 - k;
    // tail back toward the center
    for (let s = 0; s <= 1; s += 0.2) {
      const tx = x - Math.cos(a) * size * 2.2 * s, ty = y - Math.sin(a) * size * 2.2 * s, rr = size * (1 - s * 0.8);
      p.circle(tx, ty, rr, s < 0.3 ? (heat > 0.6 ? C[5] : C[4]) : heat > 0.5 ? C[3] : C[2]);
    }
    p.light(x, y, C[3], 0.45 * heat);
  });

  // embers: slow drifting specks that blink out
  burst(9, o.seed + 3, (r) => {
    const a = r.range(0, TAU), v = r.range(40, 90), life = r.range(0.28, 0.38), d0 = r.range(0.04, 0.1);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, tt, 0, 3);
    p.px(x, y, tt < life * 0.6 ? C[5] : C[3]);
    p.light(x, y, C[4], 0.35 * (1 - tt / life));
  });
}
