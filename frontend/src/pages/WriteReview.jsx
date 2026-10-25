import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api.js';
import { LISTING_TYPES, TAGS } from '../constants.js';

const ACCEPT = 'image/jpeg,image/png,image/webp,application/pdf';

export default function WriteReview() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const listingId = params.get('listing');
  const [listing, setListing] = useState(null);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setListing(null);
    if (listingId) api.get(`/listings/${listingId}`).then((d) => setListing(d.listing), (e) => setError(e.message));
  }, [listingId]);

  const onSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const res = await api.postForm('/reviews', new FormData(e.currentTarget));
      navigate('/me', { state: { flash: res.message } });
    } catch (err) {
      setError(err.message);
      window.scrollTo(0, 0);
    } finally {
      setSubmitting(false);
    }
  };

  const today = new Date().toISOString().slice(0, 10);

  return (
    <>
      <h1>Write a review</h1>
      <p className="muted">Tell it like it happened. A moderator checks every review before it goes live, usually within 24 hours.</p>
      {error && <div className="error" role="alert">{error}</div>}

      <form className="card form" onSubmit={onSubmit}>
        {listingId ? (
          <>
            <input type="hidden" name="listingId" value={listingId} />
            <p className="reviewing">Reviewing <strong>{listing?.name ?? '...'}</strong> · <Link to="/write">review something else</Link></p>
          </>
        ) : (
          <fieldset>
            <legend>What are you reviewing?</legend>
            <label>Type
              <select name="listingType" defaultValue="Product">
                {Object.entries(LISTING_TYPES).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </label>
            <label>Link to the product or company page <span className="muted">(optional, helps others find it)</span>
              <input name="listingUrl" type="url" defaultValue={params.get('url') ?? ''} placeholder="https://www.amazon.in/dp/..." />
            </label>
            <label>Name
              <input name="listingName" maxLength={150} placeholder="e.g. boAt Airdopes 141, or Swiggy Instamart" />
            </label>
            <p className="hint">If the link is already on Unfiltered, your review is added to that page and the name is ignored.</p>
          </fieldset>
        )}

        <fieldset className="rating">
          <legend>Your rating</legend>
          {[5, 4, 3, 2, 1].map((i) => (
            <label key={i}><input type="radio" name="rating" value={i} required /> {i}&#9733;</label>
          ))}
        </fieldset>

        <label>Title
          <input name="title" required minLength={5} maxLength={120} placeholder="Sum up what happened in one line" />
        </label>

        <label>Your review
          <textarea name="body" required minLength={50} maxLength={5000} rows={8} placeholder="What did you buy, what went wrong or right, how did the seller or support respond?" />
        </label>
        <p className="hint">Describe your own experience. Don't include phone numbers, addresses or full names of staff: we remove them automatically.</p>

        <fieldset>
          <legend>What is it about?</legend>
          <div className="checks">
            {Object.entries(TAGS).map(([k, v]) => (
              <label key={k} className="check"><input type="checkbox" name="tags" value={k} /> {v}</label>
            ))}
          </div>
        </fieldset>

        <div className="row">
          <label>Bought on <span className="muted">(site or app)</span>
            <input name="boughtOn" maxLength={60} placeholder="Amazon, Flipkart, in store..." />
          </label>
          <label>Purchase date
            <input name="purchaseDate" type="date" max={today} />
          </label>
        </div>

        <fieldset>
          <legend>Proof of purchase <span className="muted">(optional, earns the Verified badge)</span></legend>
          <input type="file" name="proof" accept={ACCEPT} />
          <p className="hint">Invoice or order screenshot, JPG, PNG, WEBP or PDF up to 5 MB. Only moderators see it, and it is deleted 90 days after review. Cover your address and phone number if you can.</p>
        </fieldset>

        <fieldset>
          <legend>Was this review rejected or removed somewhere else?</legend>
          <label className="check"><input type="checkbox" name="rejectedElsewhere" value="true" /> Yes, the original site rejected or removed it</label>
          <input type="file" name="rejection" accept={ACCEPT} />
          <p className="hint">Attach a screenshot of the rejection email or message to get the "Rejected elsewhere" badge.</p>
        </fieldset>

        <label className="check agree">
          <input type="checkbox" name="agree" value="true" required /> This is my own honest experience, and I have not been paid or offered anything for this review.
        </label>

        <button className="btn" disabled={submitting}>{submitting ? 'Submitting...' : 'Submit for review'}</button>
      </form>
    </>
  );
}
