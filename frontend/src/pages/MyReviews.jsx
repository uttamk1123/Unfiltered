import { Link } from 'react-router-dom';
import { useApi } from '../api.js';
import { day } from '../constants.js';
import { ErrorBox, Loading } from '../components/Status.jsx';

export default function MyReviews() {
  const { data, error, loading } = useApi('/reviews/mine');

  return (
    <>
      <h1>My reviews</h1>
      {loading && <Loading />}
      {error && <ErrorBox error={error} />}

      {data && !data.length && (
        <div className="card empty">
          <p>You haven't written any reviews yet.</p>
          <Link className="btn" to="/write">Write your first review</Link>
        </div>
      )}

      {data?.length > 0 && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Review</th><th>About</th><th>Status</th><th>Submitted</th></tr></thead>
            <tbody>
              {data.map((r) => (
                <tr key={r.id}>
                  <td><Link to={`/reviews/${r.id}`}>{r.title}</Link></td>
                  <td><Link to={`/listings/${r.listingId}`}>{r.listingName}</Link></td>
                  <td>
                    <span className={`badge status-${r.status.toLowerCase()}`}>{r.status}</span>
                    {r.status === 'Rejected' && r.lastReason && <div className="hint">{r.lastReason}</div>}
                  </td>
                  <td>{day(r.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
