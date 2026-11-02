import { burst, seg, ease, fit, star, hex, WHITE } from "../../core/index.js";

// A top-down bomb pop for Explode It in a cartoon key: a jagged two-tone burst star slams in, overshoots and snaps away,
// speed lines shoot out past the radius and a ring of round puffs pops. The shortest of the three, for a sticker-book look.
export const meta = {
  name: "Bomb pop (cartoon)",
  desc: "Top-down cartoon bomb pop: an inked two-tone burst star slams in and snaps away, speed lines shoot out and a ring of round puffs pops, gone in 0.36 s.",
  tags: ["explosion", "bomb", "top-down", "cartoon", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.36,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a1418", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#fff3a6", "#ffffff"], radius: 40, seed: 2, outline: "#1a1422", puff: "#d8d0e0" },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 7), R = o.radius, ink = hex(o.outline), puff = hex(o.puff);

  // round puffs on a ring: pop up, hold a beat, shrink to nothing
  burst(9, o.seed + 3, (r, i) => {
    const a = (i / 9) * Math.PI * 2 + r.range(-0.2, 0.2), d = R * r.range(0.62, 0.8), s = r.range(4, 6.5);
    const k = seg(t, 0.04 + r.range(0, 0.03), 0.34);
    if (k <= 0 || k >= 1) return;
    const rr = s * (k < 0.25 ? ease.outBack(k / 0.25) : 1 - ease.inQuad((k - 0.25) / 0.75));
    if (rr < 0.5) return;
    const x = Math.cos(a) * d * (0.85 + 0.15 * k), y = Math.sin(a) * d * (0.85 + 0.15 * k);
    p.circle(x, y, rr + 1, ink);
    p.circle(x, y, rr, puff);
    p.circle(x - rr * 0.25, y - rr * 0.3, rr * 0.55, WHITE);
  });

  // the burst star: white slam, overshoot, then two-tone, then snaps away
  const k = seg(t, 0, 0.24);
  if (k < 1) {
    // starts at 0.6 so the very first frame already slams
    const sc = k < 0.15 ? (0.6 + 0.4 * ease.outBack(k / 0.15)) * 1.15 : 1.15 - 0.15 * seg(k, 0.15, 0.35) - 0.9 * ease.inQuart(seg(k, 0.5, 1));
    const rot = t * 1.6, outer = R * 0.62 * sc, inner = outer * 0.55;
    if (outer > 1) {
      p.poly(star(0, 0, inner + 2, outer + 2, 9, rot), ink);
      p.poly(star(0, 0, inner, outer, 9, rot), k < 0.12 ? WHITE : C[4]);
      p.poly(star(0, 0, inner * 0.6, outer * 0.62, 9, rot + 0.17), k < 0.2 ? WHITE : C[5]);
    }
  }

  // speed lines shooting out past the radius
  burst(12, o.seed, (r, i) => {
    const a = (i / 12) * Math.PI * 2 + r.range(-0.15, 0.15), d0 = r.range(0, 0.03);
    const u = seg(t, 0.02 + d0, 0.2 + d0);
    if (u <= 0 || u >= 1) return;
    const r0 = R * (0.55 + 0.5 * ease.outQuad(u)), r1 = Math.min(R * 1.15, r0 + R * 0.35 * (1 - u));
    p.line(Math.cos(a) * r0, Math.sin(a) * r0, Math.cos(a) * r1, Math.sin(a) * r1, u < 0.4 ? WHITE : C[5]);
  });

  const pop = Math.exp(-t / 0.05);
  p.glow(0, 0, R, C[4], 0.35 * pop);
}
