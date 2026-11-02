import { draw as wakeDraw } from "./fireball-wake.js";

// The awakened king's lunge trail for Explode It: the same cooling fire puff as fireball-wake, sized to the king's
// body and in his rage colors, dropped along the lunge so the charge leaves a burning afterimage behind it.
// A bigger canvas instead of scaling the small sheet keeps the pixels the same size as the rest.
export const meta = {
  name: "Lunge wake",
  desc: "Top-down lunge afterimage: a king-sized hot puff that shrinks, cools to red and soot and sheds a spark, gone in 0.3 s.",
  tags: ["fire", "trail", "dash", "top-down", "boss", "one-shot"],
  size: [64, 64],
  origin: [32, 32],
  duration: 0.3,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#2a0f14", "#7a1420", "#c9281f", "#ff5a2a", "#ffa040", "#ffe08a"], radius: 16, seed: 2 },
};

export function draw(p, t, o) {
  wakeDraw(p, t, o);
}
