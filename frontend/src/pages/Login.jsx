import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth.jsx';

// Only allow redirects back into this site after login.
export const safeNext = (next) => (next && /^\/(?![/\\])/.test(next) && !next.includes('\\') ? next : '/');

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const next = safeNext(params.get('next'));
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState(null);

  const onSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      await login(email, password);
      navigate(next, { replace: true });
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="narrow">
      <h1>Log in</h1>
      {error && <div className="error" role="alert">{error}</div>}
      <form className="card form" onSubmit={onSubmit}>
        <label>Email <input type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} /></label>
        <label>Password <input type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} /></label>
        <button className="btn">Log in</button>
      </form>
      <p className="muted">New here? <Link to={`/register?next=${encodeURIComponent(next)}`}>Create an account</Link></p>
    </div>
  );
}
