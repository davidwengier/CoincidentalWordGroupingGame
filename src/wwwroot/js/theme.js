const storageKey = "theme";
const darkTheme = "dark";
const lightTheme = "light";

export function initializeTheme() {
  const storedTheme = window.localStorage.getItem(storageKey);
  const initialTheme =
    storedTheme === darkTheme || storedTheme === lightTheme
      ? storedTheme
      : (window.matchMedia("(prefers-color-scheme: dark)").matches ? darkTheme : lightTheme);

  applyTheme(initialTheme);
  return initialTheme === darkTheme;
}

export function toggleTheme() {
  const nextTheme = getActiveTheme() === darkTheme ? lightTheme : darkTheme;
  window.localStorage.setItem(storageKey, nextTheme);
  applyTheme(nextTheme);
  return nextTheme === darkTheme;
}

function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
}

function getActiveTheme() {
  return document.documentElement.dataset.theme === darkTheme ? darkTheme : lightTheme;
}
