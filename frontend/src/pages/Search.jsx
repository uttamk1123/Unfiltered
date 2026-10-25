import { Link, useSearchParams } from 'react-router-dom';
import { useApi } from '../api.js';
import { LISTING_TYPES } from '../constants.js';
import { ErrorBox, Loading } from '../components/Status.jsx';

export default function Search() {
  const [params] = useSearchParams();
  const q = params.get('q') ?? '';
  const { data, error, loading } = useApi(`/listings?q=${encodeURIComponent(q)}`);

  return (
    <>
      <h1>{q ? `Results for "${q}"` : 'Browse products and companies'}</h1>
      {loading && <Loading />}
      {error && <ErrorBox error={error} />}

      {data && !data.results.length && (
        <div className="card empty">
          <p>No reviews for this yet.</p>
          <Link className="btn" to={data.isUrl ? `/write?url=${encodeURIComponent(q)}` : '/write'}>Write the first review</Link>
        </div>
      )}

      {data?.results.map((l) => (
        <Link key={l.id} className="card listing-row" to={`/listings/${l.id}`}>
          <div>
            <strong>{l.name}</strong>
            <div className="meta">{LISTING_TYPES[l.type]}{l.platform && ` · ${l.platform}`}</div>
          </div>
          <div className="score">
            {l.score != null ? <><span className="big">{l.score}</span>/5</> : <span className="muted">No reviews</span>}
            <div className="meta">{l.reviewCount} review{l.reviewCount === 1 ? '' : 's'}</div>
          </div>
        </Link>
      ))}
    </>
  );
}
