import { Link } from 'react-router-dom';
import { TAGS, day } from '../constants.js';

export function Stars({ rating }) {
  return (
    <span className="stars" aria-label={`${rating} out of 5`}>
      {'★'.repeat(rating)}{'☆'.repeat(5 - rating)}
    </span>
  );
}

export function Badges({ review }) {
  return (
    <div className="badges">
      {review.verified && <span className="badge good">Verified purchase</span>}
      {review.rejectedElsewhere && <span className="badge warn">Rejected elsewhere</span>}
      {review.tags.map((t) => <span key={t} className="badge">{TAGS[t] ?? t}</span>)}
    </div>
  );
}

export function ReviewMeta({ review, showListing }) {
  return (
    <div className="meta">
      {showListing && <><Link to={`/listings/${review.listingId}`}>{review.listingName}</Link> · </>}
      by {review.displayName} · {day(review.publishedAt ?? review.createdAt)}
      {review.boughtOn && <> · bought on {review.boughtOn}</>}
    </div>
  );
}

export default function ReviewCard({ review, showListing = false }) {
  return (
    <article className="card review">
      <div className="review-head">
        <Stars rating={review.rating} />
        <Link className="review-title" to={`/reviews/${review.id}`}>{review.title}</Link>
      </div>
      <ReviewMeta review={review} showListing={showListing} />
      <Badges review={review} />
      <p className="body clamp">{review.body}</p>
    </article>
  );
}
