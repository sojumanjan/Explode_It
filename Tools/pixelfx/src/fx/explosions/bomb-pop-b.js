import { burst, fly, seg, ease, fit, at, WHITE } from "../../core/index.js";

// A top-down bomb pop for Explode It: a fireball that blooms out of round flame puffs, cools edge-first into dark
// smoke and shrinks away. Chunkier and warmer than the ring version, still about half a second.
export const meta = {
  name: "Bomb pop (fireball)",
  desc: "Top-down bomb blast: a white pop, a bloom of round flame puffs thrown out to the radius that cool into dark smoke and shrink away, with hot chips flung past, gone in 0.5 s.",
  tags: ["explosion", "bomb", "top-down", "fire", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.5,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a1418", "#4a1712", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#fff3a6", "#ffffff"], radius: 40, seed: 5 },
};

const SMOKE = [0x2a2430, 0x3b3242, 0x4f4556];

export function draw(p, t, o) {
  const C = fit(o.palette, 8), R = o.radius;
  // first beat in the ramp's brightest step, not white: a big white disc on every blast strained the eyes (user call)
  if (t < 0.034) p.circle(0, 0, R * 0.35, C[7]);

  // flame puffs: thrown out fast, each a lit ball whose color steps down the ramp as it ages
  const puffs = [];
  // spread evenly round the circle with jitter, so the bloom reads round from above
  burst(16, o.seed, (r, i) => {
    puffs.push({ a: (i / 16) * Math.PI * 2 + r.range(-0.25, 0.25), d: r.range(0.15, 0.75) * R, life: r.range(0.3, 0.46), s: r.range(5, 10) });
  });
  // far ones first so the center reads on top
  puffs.sort((u, v) => v.d - u.d);
  for (const q of puffs) {
    const k = seg(t, 0.01, q.life);
    if (k >= 1) continue;
    const out = ease.outExpo(Math.min(1, k * 1.6)), x = Math.cos(q.a) * q.d * out, y = Math.sin(q.a) * q.d * out;
    const r = q.s * (0.6 + 0.6 * Math.min(1, k * 3)) * (1 - ease.inQuad(k) * 0.8);
    if (k > 0.55) {
      // cooled to smoke: a dark disc lit on top, thinning
      const fade = 1 - seg(k, 0.72, 1);
      p.disc(x, y, r, SMOKE[1], fade * 0.9);
      p.disc(x - r * 0.25, y - r * 0.3, r * 0.6, SMOKE[2], fade * 0.8);
    } else {
      const heat = 1 - k / 0.55;
      p.circle(x, y, r, at(C.slice(2, 6), heat));
      p.circle(x - r * 0.2, y - r * 0.25, r * 0.65, at(C.slice(4), heat));
      if (heat > 0.6) p.circle(x - r * 0.25, y - r * 0.3, r * 0.3, WHITE);
      p.glow(x, y, r * 2, C[5], 0.25 * heat);
    }
  }

  // hot chips flung past the flame
  burst(10, o.seed + 9, (r) => {
    const a = r.range(0, Math.PI * 2), v = r.range(180, 340), life = r.range(0.2, 0.34);
    if (t > life) return;
    const [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, t, 0, 6);
    p.px(x, y, t < life * 0.5 ? C[6] : C[4]);
    p.light(x, y, C[5], 0.5 * (1 - t / life));
  });

  const pop = Math.exp(-t / 0.08);
  p.glow(0, 0, R * 1.2, C[4], 0.45 * pop);
}
