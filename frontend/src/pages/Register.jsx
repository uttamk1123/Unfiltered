import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth.jsx';
import { safeNext } from './Login.jsx';

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [form, setForm] = useState({ email: '', displayName: '', password: '' });
  const [error, setError] = useState(null);
  const set = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  const onSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      await register(form);
      navigate(safeNext(params.get('next')), { replace: true, state: { flash: `Welcome, ${form.displayName.trim()}. Only your public name is shown on reviews.` } });
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="narrow">
      <h1>Create an account</h1>
      {error && <div className="error" role="alert">{error}</div>}
      <form className="card form" onSubmit={onSubmit}>
        <label>Email <span className="muted">(never shown publicly)</span>
          <input type="email" required autoComplete="email" value={form.email} onChange={set('email')} />
        </label>
        <label>Public name <span className="muted">(a nickname is fine)</span>
          <input required minLength={2} maxLength={40} value={form.displayName} onChange={set('displayName')} />
        </label>
        <label>Password <span className="muted">(at least 8 characters)</span>
          <input type="password" required minLength={8} autoComplete="new-password" value={form.password} onChange={set('password')} />
        </label>
        <button className="btn">Sign up</button>
      </form>
      <p className="muted">Already have an account? <Link to="/login">Log in</Link></p>
    </div>
  );
}
