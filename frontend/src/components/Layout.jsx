import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../auth.jsx';

export default function Layout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [q, setQ] = useState('');
  const flash = location.state?.flash;

  // Keep the header search box in sync with the search page's query.
  useEffect(() => {
    setQ(location.pathname === '/search' ? params.get('q') ?? '' : '');
  }, [location.pathname, params]);

  useEffect(() => window.scrollTo(0, 0), [location.pathname]);

  const onSearch = (e) => {
    e.preventDefault();
    navigate(`/search?q=${encodeURIComponent(q.trim())}`);
  };

  const onLogout = async () => {
    await logout();
    navigate('/', { state: { flash: 'You are logged out.' } });
  };

  return (
    <>
      <header className="topbar">
        <div className="wrap topbar-inner">
          <Link className="brand" to="/">Unfiltered</Link>
          <form className="search" role="search" onSubmit={onSearch}>
            <input aria-label="Search" placeholder="Search a product or company, or paste a link" value={q} onChange={(e) => setQ(e.target.value)} />
          </form>
          <nav>
            <Link className="btn" to="/write">Write a review</Link>
            {user ? (
              <>
                {user.role === 'Moderator' && <Link to="/moderation">Moderation</Link>}
                <Link to="/me">My reviews</Link>
                <button className="link" onClick={onLogout}>Log out</button>
              </>
            ) : (
              <>
                <Link to="/login">Log in</Link>
                <Link to="/register">Sign up</Link>
              </>
            )}
          </nav>
        </div>
      </header>
      <main className="wrap">
        {flash && <div className="flash" role="status">{flash}</div>}
        <Outlet />
      </main>
      <footer className="wrap foot">
        Unfiltered never takes money to hide, remove or reorder reviews. <Link to="/policy">Content policy</Link>
      </footer>
    </>
  );
}
