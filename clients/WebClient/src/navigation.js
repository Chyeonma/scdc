const navigationEvent = 'scdc:navigate';

export const pageNavigation = {
  getSnapshot: () => window.location.pathname,
  subscribe(listener) {
    window.addEventListener('popstate', listener);
    window.addEventListener(navigationEvent, listener);
    return () => {
      window.removeEventListener('popstate', listener);
      window.removeEventListener(navigationEvent, listener);
    };
  },
};

export function navigateTo(path, { replace = false } = {}) {
  if (window.location.pathname + window.location.search + window.location.hash === path) return;
  window.history[replace ? 'replaceState' : 'pushState'](null, '', path);
  window.dispatchEvent(new Event(navigationEvent));
}
