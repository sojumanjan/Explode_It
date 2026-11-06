import { cycle, fit, mix, TAU } from "../../core/index.js";

// Explode It: soft heat haze welling up round the king while he trembles (awakening and every phase shift). It sits on
// top of the square ember particles he already throws, so it is haze only: no flames (user call, 11/6). Built round a
// body-sized ellipse with feet at (0, 0). Wisps are born on and just outside the outline and rise off it; over the
// body and face they stay nearly clear so the armor and expression read. Everything is born through `cycle`: seamless.
export const meta = {
  name: "Rage haze",
  desc: "Soft heat haze welling up round a trembling body: translucent lit wisps born on its outline rise and thin away, staying clear of the body itself, in a seamless loop.",
  tags: ["fire", "rage", "aura", "haze", "boss", "loop"],
  size: [144, 168],
  origin: [72, 158],
  duration: 1.2,
  loop: true,
  fps: 24,
  bg: "ground",
  options: { palette: ["#3a0d0d", "#8c1a14", "#d8361e", "#ff6a2a", "#ffb347", "#fff0a0"], rx: 30, ry: 40, count: 60, seed: 5 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 6), RX = o.rx, RY = o.ry, CY = -RY, P = meta.duration;
  const face = (x, y) => Math.abs(x) < RX * 0.55 && y > CY - RY * 0.75 && y < CY + RY * 0.05;

  // a faint warm pool at the feet that breathes with the loop
  const breathe = 0.5 + 0.5 * Math.sin((t / P) * TAU * 2);
  p.glow(0, -2, RX * 1.6, C[3], 0.08 + 0.05 * breathe);

  // wisps: mid ramp steps, lit on top. Yellow over his dark armor turns olive, so the body stays nearly clear
  cycle(t, P, o.count, o.seed + 4, (g) => g.range(0.7, 1.1), (age, life, r) => {
    const k = age / life, a = r.range(0, TAU), d = r.range(0.95, 1.2);
    const x0 = Math.cos(a) * RX * d, y0 = CY + Math.sin(a) * RY * d;
    const x = x0 + Math.sin(age * 4 + a) * 2, y = y0 - r.range(14, 26) * k;
    const inside = Math.hypot(x / RX, (y - CY) / RY) < 0.9;
    const rad = r.range(6, 11) * (0.6 + 0.6 * k), fade = Math.sin(Math.PI * k) * (inside || face(x, y) ? 0.25 : 1);
    const c = mix(C[2], C[3], r.range(0.4, 1));
    p.disc(x, y, rad, c, (q) => 0.22 * fade * (1 - q * q));
    p.disc(x - rad * 0.2, y - rad * 0.3, rad * 0.55, C[4], (q) => 0.13 * fade * (1 - q * q));
    p.light(x, y, C[4], 0.06 * fade);
  });
}
