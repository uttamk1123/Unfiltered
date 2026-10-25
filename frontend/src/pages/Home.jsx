import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useApi } from '../api.js';
import ReviewCard from '../components/ReviewCard.jsx';
import { ErrorBox, Loading } from '../components/Status.jsx';

export default function Home() {
  const { data, error, loading } = useApi('/reviews/home');
  const [q, setQ] = useState('');
  const navigate = useNavigate();

  return (
    <>
      <section className="hero">
        <h1>The reviews shopping sites wouldn't publish.</h1>
        <p>Review rejected, hidden, or nowhere to post it? Share your real experience with a product, seller or service. Checked against proof of purchase, never for sale.</p>
        <form className="hero-search" onSubmit={(e) => { e.preventDefault(); navigate(`/search?q=${encodeURIComponent(q.trim())}`); }}>
          <input aria-label="Search" required placeholder="Paste an Amazon or Flipkart link, or search a company" value={q} onChange={(e) => setQ(e.target.value)} />
          <button className="btn">Search</button>
        </form>
        {data && <p className="muted">{data.publishedCount} published reviews · {data.rejectedElsewhereCount} were rejected elsewhere first</p>}
      </section>

      <h2>Latest reviews</h2>
      {loading && <Loading />}
      {error && <ErrorBox error={error} />}
      {data && !data.recent.length && <p className="muted">No reviews yet. <Link to="/write">Be the first.</Link></p>}
      {data?.recent.map((r) => <ReviewCard key={r.id} review={r} showListing />)}
    </>
  );
}
