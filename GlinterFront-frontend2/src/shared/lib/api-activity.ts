type Listener = () => void;

let activeRequests = 0;
const listeners = new Set<Listener>();

const emitChange = () => {
  listeners.forEach((listener) => listener());
};

export const apiActivity = {
  begin: (): (() => void) => {
    activeRequests += 1;
    emitChange();

    let completed = false;
    return () => {
      if (completed) return;

      completed = true;
      activeRequests = Math.max(0, activeRequests - 1);
      emitChange();
    };
  },

  subscribe: (listener: Listener): (() => void) => {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  getSnapshot: (): number => activeRequests,
};
