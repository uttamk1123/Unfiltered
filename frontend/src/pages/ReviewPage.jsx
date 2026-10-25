import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, useApi } from '../api.js';
import { useAuth } from '../auth.jsx';
import { day } from '../constants.js';
import { Badges, Stars } from '../components/ReviewCard.jsx';
import { ErrorBox, Loading } from '../components/Status.jsx';

const STATUS_TEXT = {
  Pending: 'Waiting for moderation. Only you and moderators can see this.',
  Rejected: 'Not published. See the reason in the history below.',
  Removed: 'This review was removed. The reason is in the history below.',
};

export default function ReviewPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const { data, error, loading } = useApi(`/reviews/${id}`);
  const [reason, setReason] = useState('');
  const [reportResult, setReportResult] = useState(null);

  if (error) return <ErrorBox error={error} />;
  if (!data) return loading ? <Loading /> : null;

  const { review, showBody, isOwner, log } = data;

  const report = async (e) => {
    e.preventDefault();
    try {
      const res = await api.post(`/reviews/${review.id}/flags`, { reason });
      setReportResult({ ok: true, message: res.message });
      setReason('');
    } catch (err) {
      setReportResult({ ok: false, message: err.message });
    }
  };

  const remove = async () => {
    if (!window.confirm('Remove your review? This cannot be undone.')) return;
    try {
      await api.del(`/reviews/${review.id}`);
      navigate('/me', { state: { flash: 'Your review was removed.' } });
    } catch (err) {
      setReportResult({ ok: false, message: err.message });
    }
  };

  return (
    <>
      <p className="meta"><Link to={`/listings/${review.listingId}`}>&larr; {review.listingName}</Link></p>

      {review.status !== 'Published' && <div className={`status-banner status-${review.status.toLowerCase()}`}>{STATUS_TEXT[review.status]}</div>}

      {showBody && (
        <article className="card review">
          <div className="review-head">
            <Stars rating={review.rating} />
            <h1 className="review-title">{review.title}</h1>
          </div>
          <div className="meta">
            by {review.displayName} · {day(review.publishedAt ?? review.createdAt)}
            {review.boughtOn && <> · bought on {review.boughtOn}</>}
            {review.purchaseDate && <> · purchased {review.purchaseDate}</>}
          </div>
          <Badges review={review} />
          <p className="body">{review.body}</p>
        </article>
      )}

      <section className="card">
        <h2>Moderation history</h2>
        {!log.length && <p className="muted">No decisions yet.</p>}
        <ul className="log">
          {log.map((l, i) => (
            <li key={i}><span className="muted">{day(l.createdAt)}</span> <strong>{l.action}</strong>{l.reason && `: ${l.reason}`}</li>
          ))}
        </ul>
        <p className="hint">Every decision about a review is shown here. Businesses cannot pay to change it.</p>
      </section>

      {reportResult && <div className={reportResult.ok ? 'flash' : 'error'} role="status">{reportResult.message}</div>}

      {review.status === 'Published' && user && !isOwner && (
        <details className="card">
          <summary>Report this review</summary>
          <form className="form" onSubmit={report}>
            <label>Why does it break the <Link to="/policy">content policy</Link>?
              <textarea required minLength={5} maxLength={1000} rows={3} value={reason} onChange={(e) => setReason(e.target.value)} />
            </label>
            <button className="btn secondary">Send report</button>
          </form>
        </details>
      )}

      {isOwner && review.status !== 'Removed' && <button className="link danger" onClick={remove}>Remove my review</button>}
    </>
  );
}
