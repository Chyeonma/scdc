// Read before React mounts: StrictMode may run state initializers twice.
export function takeAccountLink(browser) {
  const kind = browser.location.pathname === '/auth/verify' ? 'verify'
    : browser.location.pathname === '/auth/reset' ? 'reset' : null;
  if (!kind) return null;
  const fragment = new URLSearchParams(browser.location.hash.slice(1));
  const token = fragment.get('token') || '';
  browser.history.replaceState(null, '', browser.location.pathname);
  return { kind, token };
}
