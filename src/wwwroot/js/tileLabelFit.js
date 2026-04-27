const minimumFontSizePx = 4.5;
const searchPrecisionPx = 0.1;

let resizeHandlerRegistered = false;
let resizeTimer;

export function initializeTileLabelFit() {
  if (resizeHandlerRegistered) {
    return;
  }

  window.addEventListener("resize", onWindowResize);
  resizeHandlerRegistered = true;
}

export function fitTileLabels() {
  for (const label of document.querySelectorAll(".tile-label")) {
    fitTileLabel(label);
  }
}

export function disposeTileLabelFit() {
  if (!resizeHandlerRegistered) {
    return;
  }

  window.removeEventListener("resize", onWindowResize);
  resizeHandlerRegistered = false;

  if (resizeTimer) {
    window.clearTimeout(resizeTimer);
    resizeTimer = undefined;
  }
}

function onWindowResize() {
  if (resizeTimer) {
    window.clearTimeout(resizeTimer);
  }

  resizeTimer = window.setTimeout(() => {
    fitTileLabels();
    resizeTimer = undefined;
  }, 50);
}

function fitTileLabel(label) {
  label.style.fontSize = "";

  const maximumFontSizePx = Number.parseFloat(window.getComputedStyle(label).fontSize);
  if (!Number.isFinite(maximumFontSizePx)) {
    return;
  }

  if (label.scrollWidth <= label.clientWidth) {
    return;
  }

  let low = minimumFontSizePx;
  let high = maximumFontSizePx;
  let best = low;

  while (high - low > searchPrecisionPx) {
    const mid = (low + high) / 2;
    label.style.fontSize = `${mid}px`;

    if (label.scrollWidth <= label.clientWidth) {
      best = mid;
      low = mid;
    } else {
      high = mid;
    }
  }

  label.style.fontSize = `${best}px`;
}
