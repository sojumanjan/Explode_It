import { draw as slashDraw } from "./topdown-slash.js";

// The awakened king's sword sweep for Explode It: the same top-down crescent as topdown-slash, drawn at the king's
// reach and arc so the swing covers exactly what it hits (SlashRadius 3.5 units = 70 px, SlashAngle 130 degrees),
// in gold and white. A bigger canvas instead of scaling the small sheet keeps the pixels the same size as the rest.
export const meta = {
  name: "Royal slash",
  desc: "Top-down royal sword sweep: a gold and white crescent at the king's reach whips round, its tail catches up as it thins and chips fly off, gone in 0.3 s.",
  tags: ["melee", "slash", "sword", "top-down", "boss", "directional", "one-shot"],
  size: [176, 176],
  origin: [88, 88],
  duration: 0.3,
  loop: false,
  fps: 30,
  bg: "dark",
  options: { palette: ["#3a2a10", "#8a6a2a", "#e8d08a", "#fff2b0", "#ffffff"], radius: 70, sweep: 2.269, seed: 4 },
};

export function draw(p, t, o) {
  slashDraw(p, t, o);
}
