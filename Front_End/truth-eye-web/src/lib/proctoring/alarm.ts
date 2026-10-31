let ctx: AudioContext | null = null;

// Call once from a user click (the "Enable camera & begin" button) so the browser allows sound later
export function unlockAlarm() {
  try {
    ctx ??= new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
    if (ctx.state === "suspended") void ctx.resume();
  } catch {
    /* audio not supported: the alarm is simply skipped */
  }
}

// Two-tone siren for 3 seconds
export function playAlarm(seconds = 3) {
  try {
    if (!ctx) return;
    if (ctx.state === "suspended") void ctx.resume();

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = "square";
    osc.connect(gain);
    gain.connect(ctx.destination);

    const t0 = ctx.currentTime;
    // alternate 880Hz / 660Hz every 0.25s
    for (let t = 0; t < seconds; t += 0.25) {
      osc.frequency.setValueAtTime(Math.floor(t / 0.25) % 2 === 0 ? 880 : 660, t0 + t);
    }
    gain.gain.setValueAtTime(0.0001, t0);
    gain.gain.exponentialRampToValueAtTime(0.25, t0 + 0.05);
    gain.gain.setValueAtTime(0.25, t0 + seconds - 0.2);
    gain.gain.exponentialRampToValueAtTime(0.0001, t0 + seconds);

    osc.start(t0);
    osc.stop(t0 + seconds);
  } catch {
    /* ignore */
  }
}