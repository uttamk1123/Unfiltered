import { useCallback, useEffect, useState } from 'react';

export class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

async function request(path, { method = 'GET', json, form } = {}) {
  const res = await fetch(`/api${path}`, {
    method,
    credentials: 'same-origin',
    headers: json !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: form ?? (json !== undefined ? JSON.stringify(json) : undefined),
  });
  if (res.status === 204) return null;
  const data = await res.json().catch(() => null);
  if (!res.ok) {
    const message = data?.error ?? data?.title ?? (res.status === 429 ? 'Too many attempts. Please wait a minute.' : `Request failed (${res.status}).`);
    throw new ApiError(message, res.status);
  }
  return data;
}

export const api = {
  get: (path) => request(path),
  post: (path, json = {}) => request(path, { method: 'POST', json }),
  postForm: (path, form) => request(path, { method: 'POST', form }),
  del: (path) => request(path, { method: 'DELETE' }),
};

/** Loads `path` whenever it changes. Returns { data, error, loading, reload }. */
export function useApi(path) {
  const [state, setState] = useState({ data: null, error: null, loading: true });

  const load = useCallback(() => {
    let cancelled = false;
    setState((s) => ({ ...s, loading: true, error: null }));
    api.get(path).then(
      (data) => !cancelled && setState({ data, error: null, loading: false }),
      (error) => !cancelled && setState({ data: null, error, loading: false }),
    );
    return () => { cancelled = true; };
  }, [path]);

  useEffect(load, [load]);
  return { ...state, reload: load };
}
