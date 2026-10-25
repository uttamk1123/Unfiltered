import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useApi } from '../api.js';
import { LISTING_TYPES, TAGS } from '../constants.js';
import ReviewCard from '../components/ReviewCard.jsx';
import { ErrorBox, Loading } from '../components/Status.jsx';

export default function ListingPage() {
  const { id } = useParams();
  const [params, setParams] = useSearchParams();
  const sort = params.get('sort') ?? 'recent';
  const verifiedOnly = params.get('verified') === '1';
  const { data, error, loading } = useApi(`/listings/${id}?sort=${sort}&verifiedOnly=${verifiedOnly}`);

  const setFilter = (key, value) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value); else next.delete(key);
    setParams(next, { replace: true });
  };

  if (error) return <ErrorBox error={error} />;
  if (!data) return loading ? <Loading /> : null;

  const { listing, distribution, topTags, reviews } = data;
  const max = Math.max(...distribution, 1);

  return (
    <>
      <section className="card listing-head">
        <div>
          <div className="meta">{LISTING_TYPES[listing.type]}{listing.platform && ` · ${listing.platform}`}</div>
          <h1>{listing.name}</h1>
          {listing.url && <a href={listing.url} target="_blank" rel="nofollow noopener noreferrer">Open the original listing</a>}
        </div>
        <div className="score-box">
          {listing.score != null ? <div><span className="big">{listing.score}</span>/5</div> : <div className="muted">No reviews yet</div>}
          <div className="meta">{listing.reviewCount} reviews · {listing.verifiedCount} verified · {listing.rejectedElsewhereCount} rejected elsewhere</div>
          <Link className="btn" to={`/write?listing=${listing.id}`}>Review this</Link>
        </div>
      </section>

      {listing.reviewCount > 0 && (
        <section className="grid2">
          <div className="card">
            <h3>Ratings</h3>
            {distribution.map((n, i) => (
              <div className="bar-row" key={i}>
                <span>{5 - i}&#9733;</span>
                <div className="bar"><div style={{ width: `${Math.round((n / max) * 100)}%` }} /></div>
                <span>{n}</span>
              </div>
            ))}
            <p className="hint">Score counts verified purchases twice as much as unverified ones.</p>
          </div>
          <div className="card">
            <h3>What people talk about</h3>
            {!topTags.length && <p className="muted">No topics tagged yet.</p>}
            <ul className="tag-list">
              {topTags.map((t) => <li key={t.tag}><span>{TAGS[t.tag] ?? t.tag}</span><span className="muted">{t.count}</span></li>)}
            </ul>
          </div>
        </section>
      )}

      <div className="filters">
        <label>Sort{' '}
          <select value={sort} onChange={(e) => setFilter('sort', e.target.value === 'recent' ? '' : e.target.value)}>
            <option value="recent">Most recent</option>
            <option value="lowest">Lowest rating</option>
            <option value="highest">Highest rating</option>
          </select>
        </label>
        <label className="check">
          <input type="checkbox" checked={verifiedOnly} onChange={(e) => setFilter('verified', e.target.checked ? '1' : '')} /> Verified purchases only
        </label>
      </div>

      {!reviews.length && listing.reviewCount > 0 && <p className="muted">No reviews match these filters.</p>}
      {reviews.map((r) => <ReviewCard key={r.id} review={r} />)}
    </>
  );
}
