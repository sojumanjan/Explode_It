import { burst, fly, seg, ease, fit, TAU } from "../../core/index.js";

// A top-down dirt burst for Explode It: the drill miner's mound erupts. Seen from above, clods of earth and stones are
// flung out to the hole's radius and drop, a brown dust cloud swells and settles, and a dark crater shadow fades.
// Every hole bursts with the same sheet, so the real hole can't be told apart by the burst.
// `radius` is the hole radius in pixels; clods land inside it so the picture matches what was hit.
// `clods` (count) and `crater` (0 or 1) let the final boss's leap slam reuse just the dust cloud.
export const meta = {
  name: "Dirt burst",
  desc: "Top-down dirt eruption: clods and stones fly out to the hole radius and drop, a brown dust cloud swells and settles and a crater shadow fades, gone in 0.6 s.",
  tags: ["explosion", "dirt", "earth", "eruption", "top-down", "boss", "one-shot"],
  size: [112, 112],
  origin: [56, 56],
  duration: 0.6,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a1d16", "#4a3426", "#6b4c36", "#8c6a4c", "#b08a66", "#d8b88e"], radius: 44, seed: 5, clods: 16, crater: 1 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6), R = o.radius;

  // crater shadow: soft and short so it doesn't read as a lingering hazard
  const ck = seg(t, 0.05, 0.5);
  if (ck < 1 && o.crater) p.disc(0, 0, R * 0.4, C[0], 0.5 * (1 - ck));

  // dust: soft discs swelling out from the middle and settling
  burst(9, o.seed + 1, (r, i) => {
    const a = (i / 9) * TAU + r.range(-0.3, 0.3), d = R * r.range(0.25, 0.7), life = r.range(0.4, 0.55), d0 = r.range(0, 0.06);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, out = ease.outQuart(Math.min(1, k * 2));
    const x = Math.cos(a) * d * out, y = Math.sin(a) * d * out, rr = 5 + 8 * ease.outQuad(k), al = 0.75 * (1 - ease.inQuad(k));
    p.disc(x, y, rr, C[3], al);
    p.disc(x - rr * 0.25, y - rr * 0.3, rr * 0.55, C[4], al * 0.8);
  });

  // clods: chunky earth thrown out to the radius, popping up toward the camera (bigger) then dropping (smaller)
  burst(o.clods, o.seed, (r, i) => {
    const a = (i / o.clods) * TAU + r.range(-0.2, 0.2), d = R * r.range(0.5, 0.95), life = r.range(0.32, 0.48), d0 = r.range(0, 0.04);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, out = ease.outQuad(k), hop = Math.sin(Math.PI * k);
    const x = Math.cos(a) * d * out, y = Math.sin(a) * d * out - hop * 6, s = r.range(1.6, 3) * (1 + hop * 0.6);
    p.disc(x + 1, y + hop * 6 + 1, s * 0.8, C[0], 0.45);
    p.circle(x, y, s, k > 0.85 ? C[1] : C[2]);
    p.px(x - s * 0.4, y - s * 0.4, C[5]);
  });
}
