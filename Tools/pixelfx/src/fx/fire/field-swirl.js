import { cycle, fit, WHITE, TAU } from "../../core/index.js";

// A top-down fire field overlay for Explode It, laid on the fire mage's spinning swirl disc. Kept quiet on purpose
// (user call: no swirl drawn over the disc): little sparks pop up here and there inside the field and fall back,
// and warm dust and ash drift slowly round with the disc's spin. Nothing touches the rim.
// Everything is born through `cycle`, so the 2 s loop wraps with no seam.
export const meta = {
  name: "Fire field embers",
  desc: "Top-down fire field overlay: small sparks pop up and fall back here and there inside the field while warm dust and ash drift slowly round, in a seamless loop.",
  tags: ["fire", "field", "embers", "dust", "top-down", "boss", "loop"],
  size: [232, 232],
  origin: [116, 116],
  duration: 2,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a0d0d", "#8c1a14", "#d8361e", "#ff6a2a", "#ffb347", "#fff0a0"], radius: 100, seed: 4 },
};

// a point spread evenly over the disc (sqrt keeps the middle from crowding), kept inside the rim
function spot(r, R) {
  const a = r.range(0, TAU), d = Math.sqrt(r.range(0.02, 1)) * R * 0.85;
  return [Math.cos(a) * d, Math.sin(a) * d];
}

export function draw(p, t, o) {
  const C = fit(o.palette, 6), R = o.radius;
  // ash is pale so it reads on the red disc; dark specks vanish into it
  const ASH = [0xb89a86, 0xe0cbb4, 0xfff0dc];

  // dust and ash: dim specks drifting slowly the way the disc turns (counterclockwise on screen), fading in and out
  cycle(t, meta.duration, 90, o.seed, 1.6, (age, life, r) => {
    const k = age / life, [x0, y0] = spot(r, R), a = Math.atan2(y0, x0) - k * 0.35, d = Math.hypot(x0, y0);
    const x = Math.cos(a) * d, y = Math.sin(a) * d - k * 3;
    const fade = Math.sin(Math.PI * k);
    const big = r.range(0, 1) < 0.3, c = ASH[Math.floor(r.range(0, 3))];
    p.blend(x, y, c, 0.85 * fade);
    if (big) { p.blend(x + 1, y, c, 0.7 * fade); p.blend(x, y + 1, ASH[0], 0.5 * fade); }
  });

  // sparks: a hot pop that jumps up a few pixels, flickers and falls back, scattered over the field
  cycle(t, meta.duration, 30, o.seed + 7, 0.45, (age, life, r) => {
    const k = age / life, [x0, y0] = spot(r, R), h = r.range(4, 9);
    const y = y0 - h * 4 * k * (1 - k), x = x0 + r.range(-3, 3) * k;
    const col = k < 0.25 ? WHITE : k < 0.6 ? C[5] : C[4];
    p.px(x, y, col);
    p.px(x + 1, y, k < 0.5 ? C[5] : C[3]);
    if (k < 0.4) { p.px(x, y + 1, C[4]); p.px(x + 1, y + 1, C[3]); }
    p.light(x, y, C[4], 0.45 * (1 - k));
  });
}
