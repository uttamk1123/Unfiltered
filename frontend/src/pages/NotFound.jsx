import { Link } from 'react-router-dom';

export default function NotFound() {
  return (
    <div className="narrow card empty">
      <p>That page does not exist.</p>
      <Link to="/">Back to the home page</Link>
    </div>
  );
}
