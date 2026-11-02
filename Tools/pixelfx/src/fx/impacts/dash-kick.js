import { burst, fly, seg, ease, fit } from "../../core/index.js";

// A top-down dash kick-off for Explode It: as a body launches to the right, a fan of dust puffs and grit is
// kicked back behind it (to the left) and thins away. Faces right; the origin is where the dash starts.
export const meta = {
  name: "Dash kick",
  desc: "Top-down dash kick-off: a fan of soft dust puffs and grit kicked back behind a body launching to the right, thinning away in 0.4 s.",
  tags: ["impact", "dust", "dash", "top-down", "directional", "one-shot"],
  size: [96, 64],
  origin: [64, 32],
  duration: 0.4,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3b2f2a", "#6b5646", "#9c8470", "#c9b49a", "#e8dcc8"], seed: 5 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 5);

  // dust puffs: soft discs kicked back in a cone, swelling and thinning, lit on top
  burst(7, o.seed, (r) => {
    const a = Math.PI + r.range(-0.7, 0.7), v = r.range(90, 170), life = r.range(0.24, 0.38), d0 = r.range(0, 0.04);
    const tt = t - d0;
    if (tt < 0 || tt > life) return;
    const k = tt / life, [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, tt, 0, 7);
    const rr = 3 + 5 * ease.outQuad(k), al = 0.75 * (1 - ease.inQuad(k));
    p.disc(x, y, rr, C[2], al);
    p.disc(x - rr * 0.25, y - rr * 0.3, rr * 0.6, C[3], al * 0.8);
  });

  // grit: hard specks flung further back
  burst(8, o.seed + 1, (r) => {
    const a = Math.PI + r.range(-0.5, 0.5), v = r.range(180, 300), life = r.range(0.14, 0.26);
    if (t > life) return;
    const [x, y] = fly(0, 0, Math.cos(a) * v, Math.sin(a) * v, t, 0, 9);
    p.px(x, y, t < life * 0.5 ? C[4] : C[1]);
  });

  // a short scuff where the feet pushed off
  const sk = seg(t, 0, 0.3);
  if (sk < 1) p.line(-2, 0, -10, 0, C[1], 0.6 * (1 - sk));
}
