import { burst, fly, seg, ease, fit, WHITE } from "../../core/index.js";

// A top-down enemy slash for Explode It: seen from above, a crescent whips round the attacker in two frames,
// its tail catches up while it thins, and a few chips fly off the leading edge. Faces right; the origin is the
// attacker's center. Short, so the next telegraph reads right away. `sweep` (radians) must equal the enemy's attack
// angle so the swing shows exactly what it hits: warrior and mantis are both 100 degrees (1.745).
export const meta = {
  name: "Top-down slash",
  desc: "Top-down melee swing: a bright crescent whips round the attacker, its tail catches up as it thins, chips fly off the tip, gone in 0.3 s.",
  tags: ["melee", "slash", "top-down", "enemy", "directional", "one-shot"],
  size: [96, 96],
  origin: [48, 48],
  duration: 0.3,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a2a40", "#7a6a88", "#c9c2d8", "#ffe9a8", "#ffffff"], radius: 34, sweep: 1.745, seed: 4 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 5), R = o.radius, a0 = -o.sweep / 2, a1 = o.sweep / 2;
  // the blade tip whips across fast; the tail follows later so the crescent shrinks to nothing at the tip
  // the tip starts a third of the way in so the very first frame already shows the blade
  const head = a0 + (a1 - a0) * (0.35 + 0.65 * ease.outQuart(seg(t, 0, 0.09)));
  const tail = a0 + (a1 - a0) * ease.inQuad(seg(t, 0.04, 0.24));
  if (head - tail > 0.02) {
    const fade = 1 - seg(t, 0.12, 0.26);
    // thick near the tip, a hair at the tail
    p.arc(0, 0, R - 3, (u) => 1 + 6 * u * fade, tail, head, C[2]);
    p.arc(0, 0, R - 1, (u) => 0.5 + 3 * u * fade, tail, head, t < 0.1 ? WHITE : C[3]);
    // a dim inner echo reads as motion blur
    p.arc(0, 0, R - 10, (u) => 2 * u * fade, tail, head, C[1], 0.6);
    p.glow(Math.cos(head) * R, Math.sin(head) * R, 14, C[3], 0.4 * fade);
  }

  // chips thrown off the leading edge, tangent to the swing
  burst(7, o.seed, (r) => {
    const born = r.range(0.03, 0.08), life = r.range(0.12, 0.2), tt = t - born;
    if (tt < 0 || tt > life) return;
    const a = a0 + (a1 - a0) * ease.outQuart(seg(born, 0, 0.09)), x0 = Math.cos(a) * R, y0 = Math.sin(a) * R;
    const dir = a + Math.PI / 2 + r.range(-0.4, 0.3), v = r.range(120, 220);
    const [x, y] = fly(x0, y0, Math.cos(dir) * v, Math.sin(dir) * v, tt, 0, 8);
    p.px(x, y, tt < life * 0.5 ? WHITE : C[3]);
    p.light(x, y, C[3], 0.5 * (1 - tt / life));
  });
}
