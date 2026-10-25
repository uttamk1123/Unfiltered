import { createContext, useContext, useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { api } from './api.js';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    api.get('/auth/me')
      .then((res) => setUser(res.user))
      .catch(() => setUser(null))
      .finally(() => setReady(true));
  }, []);

  const value = {
    user,
    ready,
    login: async (email, password) => setUser((await api.post('/auth/login', { email, password })).user),
    register: async (form) => setUser((await api.post('/auth/register', form)).user),
    logout: async () => {
      await api.post('/auth/logout');
      setUser(null);
    },
  };
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);

export function RequireAuth({ children, role }) {
  const { user, ready } = useAuth();
  const location = useLocation();
  if (!ready) return null;
  if (!user) return <Navigate to={`/login?next=${encodeURIComponent(location.pathname + location.search)}`} replace />;
  if (role && user.role !== role) return <div className="card empty narrow"><p>Only moderators can open this page.</p></div>;
  return children;
}
