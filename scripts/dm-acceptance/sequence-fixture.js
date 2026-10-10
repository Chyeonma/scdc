// Acceptance DevTools only: assign one known UUID before creating the immutable operation.
(() => {
  window.dmSequenceFixture?.off();
  const original = crypto.randomUUID.bind(crypto);
  window.dmSequenceFixture = Object.freeze({
    arm(uuid) {
      if (location.hostname !== 'localhost' || location.port !== '15300'
        || !/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(uuid))
        throw new Error('Use an acceptance UUIDv4 on localhost:15300.');
      crypto.randomUUID = () => { crypto.randomUUID = original; return uuid; };
    },
    off() { crypto.randomUUID = original; delete window.dmSequenceFixture; },
  });
})();
