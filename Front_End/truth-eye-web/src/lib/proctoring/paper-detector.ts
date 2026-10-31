// "Paper in front of the camera" check. No model is needed.
// A sheet of paper looks like: a large, bright, low-saturation region that WASN'T there at the start of the exam
// (so a white wall or a white shirt that is always in the frame is ignored), with a sharp, straight edge.

const W = 96;
const H = 72;
const CELLS_X = 12; // the frame is split into a 12 x 9 grid
const CELLS_Y = 9;

let canvas: HTMLCanvasElement | null = null;
let ctx: CanvasRenderingContext2D | null = null;

export interface PaperResult {
  found: boolean;
  coverage: number; // share of the frame that is newly bright (0..1)
  edge: number; // strength of the sharp edge around that region (0..1)
}

interface Baseline {
  // average brightness of every grid cell, learned while the exam is calm
  cells: Float32Array;
  frames: number;
}

const baselines = new WeakMap<HTMLVideoElement, Baseline>();

/** Call when a new exam starts so the learned background is cleared */
export function resetPaperDetector(video: HTMLVideoElement | null) {
  if (video) baselines.delete(video);
}

function grab(video: HTMLVideoElement) {
  if (!canvas) {
    canvas = document.createElement("canvas");
    canvas.width = W;
    canvas.height = H;
    ctx = canvas.getContext("2d", { willReadFrequently: true });
  }
  if (!ctx || video.readyState < 2) return null;
  ctx.drawImage(video, 0, 0, W, H);
  return ctx.getImageData(0, 0, W, H).data;
}

export function detectPaper(video: HTMLVideoElement): PaperResult {
  const none: PaperResult = { found: false, coverage: 0, edge: 0 };
  const data = grab(video);
  if (!data) return none;

  const cw = W / CELLS_X;
  const ch = H / CELLS_Y;
  const bright = new Float32Array(CELLS_X * CELLS_Y); // share of paper-like pixels per cell
  const lum = new Float32Array(W * H);

  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const i = (y * W + x) * 4;
      const r = data[i], g = data[i + 1], b = data[i + 2];
      const max = Math.max(r, g, b);
      const min = Math.min(r, g, b);
      const sat = max === 0 ? 0 : (max - min) / max;
      lum[y * W + x] = 0.299 * r + 0.587 * g + 0.114 * b;
      if (max > 165 && sat < 0.22) {
        bright[Math.floor(y / ch) * CELLS_X + Math.floor(x / cw)] += 1 / (cw * ch);
      }
    }
  }

  // Learn the background during the first frames (about 3 seconds), then compare against it
  let base = baselines.get(video);
  if (!base) {
    base = { cells: new Float32Array(CELLS_X * CELLS_Y), frames: 0 };
    baselines.set(video, base);
  }
  if (base.frames < 6) {
    for (let k = 0; k < bright.length; k++) base.cells[k] = (base.cells[k] * base.frames + bright[k]) / (base.frames + 1);
    base.frames++;
    return none; // still learning
  }

  // A cell is "new paper" if it is mostly bright now and wasn't before
  let newCells = 0;
  const isNew = new Uint8Array(bright.length);
  for (let k = 0; k < bright.length; k++) {
    if (bright[k] > 0.6 && base.cells[k] < 0.35) {
      isNew[k] = 1;
      newCells++;
    }
  }
  const coverage = newCells / bright.length;

  // The sharp edge: strong brightness change on the border of the new cells
  let edgeSum = 0;
  let edgeCount = 0;
  for (let cy = 0; cy < CELLS_Y; cy++) {
    for (let cx = 0; cx < CELLS_X; cx++) {
      const k = cy * CELLS_X + cx;
      if (!isNew[k]) continue;
      const neighbours = [
        cx > 0 ? k - 1 : -1,
        cx < CELLS_X - 1 ? k + 1 : -1,
        cy > 0 ? k - CELLS_X : -1,
        cy < CELLS_Y - 1 ? k + CELLS_X : -1,
      ];
      for (const n of neighbours) {
        if (n >= 0 && !isNew[n]) {
          // brightness step between the paper cell and the cell next to it
          edgeSum += Math.abs(bright[k] - bright[n]);
          edgeCount++;
        }
      }
    }
  }
  const edge = edgeCount ? edgeSum / edgeCount : 0;

  // Paper that is held up covers a good part of the frame, and its border is clear
  const found = coverage >= 0.12 && coverage <= 0.8 && edge >= 0.45;
  return { found, coverage, edge };
}