import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, useApi } from '../api.js';
import { LISTING_TYPES, TAGS } from '../constants.js';
import { Stars } from '../components/ReviewCard.jsx';
import { ErrorBox, Loading } from '../components/Status.jsx';

export default function Moderation() {
  const { data, error, loading, reload } = useApi('/moderation/queue');
  const [message, setMessage] = useState(null);

  // Runs a moderation action, shows its result and refreshes the queue.
  const act = async (path, body) => {
    try {
      const res = await api.post(path, body);
      setMessage({ ok: true, text: res.message });
      reload();
    } catch (err) {
      setMessage({ ok: false, text: err.message });
    }
  };

  if (error) return <ErrorBox error={error} />;
  if (!data) return loading ? <Loading /> : null;

  return (
    <>
      <h1>Moderation</h1>
      {message && <div className={message.ok ? 'flash' : 'error'} role="status">{message.text}</div>}

      <h2>Waiting for review ({data.pending.length})</h2>
      {!data.pending.length && <p className="muted">Queue is empty.</p>}
      {data.pending.map((r) => <PendingItem key={r.id} review={r} act={act} />)}

      <h2>Open reports ({data.flags.length})</h2>
      {!data.flags.length && <p className="muted">No open reports.</p>}
      {data.flags.map((f) => <FlagItem key={f.id} flag={f} act={act} />)}
    </>
  );
}

function FileLink({ name }) {
  return name
    ? <a href={`/api/moderation/files/${name}`} target="_blank" rel="noreferrer">open file</a>
    : <span className="muted">none</span>;
}

function PendingItem({ review: r, act }) {
  const [verified, setVerified] = useState(false);
  const [reason, setReason] = useState('');

  return (
    <article className="card mod-item">
      <div className="meta">
        #{r.id} · <strong>{r.listingName}</strong> ({LISTING_TYPES[r.listingType]}
        {r.listingUrl && <>, <a href={r.listingUrl} target="_blank" rel="nofollow noopener noreferrer">listing</a></>})
        · by {r.displayName} &lt;{r.email}&gt; · {new Date(r.createdAt).toLocaleString()}
      </div>
      <div className="review-head"><Stars rating={r.rating} /><strong>{r.title}</strong></div>
      <div className="badges">
        {r.tags.map((t) => <span key={t} className="badge">{TAGS[t] ?? t}</span>)}
        {r.boughtOn && <span className="badge">bought on {r.boughtOn}</span>}
        {r.purchaseDate && <span className="badge">purchased {r.purchaseDate}</span>}
      </div>
      <p className="body">{r.body}</p>

      {r.riskNotes.length > 0 && (
        <div className="risk">
          <strong>Auto-check notes</strong>
          <ul>{r.riskNotes.map((n) => <li key={n}>{n}</li>)}</ul>
        </div>
      )}

      <p className="files">Proof of purchase: <FileLink name={r.proofFile} /> · Rejection screenshot: <FileLink name={r.rejectionFile} /></p>

      <div className="mod-actions">
        <form onSubmit={(e) => { e.preventDefault(); act(`/moderation/reviews/${r.id}/approve`, { verified }); }}>
          <label className="check">
            <input type="checkbox" disabled={!r.proofFile} checked={verified} onChange={(e) => setVerified(e.target.checked)} /> Proof checks out: mark as verified
          </label>
          <button className="btn">Publish</button>
        </form>
        <form onSubmit={(e) => { e.preventDefault(); act(`/moderation/reviews/${r.id}/reject`, { reason }); }}>
          <input required maxLength={1000} placeholder="Reason shown to the reviewer" value={reason} onChange={(e) => setReason(e.target.value)} />
          <button className="btn danger">Reject</button>
        </form>
      </div>
    </article>
  );
}

function FlagItem({ flag: f, act }) {
  const [note, setNote] = useState('');
  const [reason, setReason] = useState('');

  return (
    <article className="card mod-item">
      <div className="meta">Report #{f.id} on <Link to={`/reviews/${f.reviewId}`}>review #{f.reviewId}</Link> · {f.listingName}</div>
      <div className="review-head"><Stars rating={f.rating} /><strong>{f.title}</strong></div>
      <p className="body clamp">{f.body}</p>
      <div className="risk"><strong>Reported because:</strong> {f.reason}</div>
      <div className="mod-actions">
        <form onSubmit={(e) => { e.preventDefault(); act(`/moderation/flags/${f.id}/dismiss`, { reason: note }); }}>
          <input maxLength={1000} placeholder="Public note (optional)" value={note} onChange={(e) => setNote(e.target.value)} />
          <button className="btn secondary">Keep review</button>
        </form>
        <form onSubmit={(e) => { e.preventDefault(); act(`/moderation/flags/${f.id}/uphold`, { reason }); }}>
          <input required maxLength={1000} placeholder="Public reason for removal" value={reason} onChange={(e) => setReason(e.target.value)} />
          <button className="btn danger">Remove review</button>
        </form>
      </div>
    </article>
  );
}
