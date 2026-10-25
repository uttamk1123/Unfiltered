/** Loading and error states shared by pages that fetch data. */
export function Loading() {
  return <p className="muted">Loading...</p>;
}

export function ErrorBox({ error }) {
  return <div className="error" role="alert">{error?.message ?? 'Something went wrong.'}</div>;
}
