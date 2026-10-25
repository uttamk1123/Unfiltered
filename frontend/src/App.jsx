import { Route, Routes } from 'react-router-dom';
import Layout from './components/Layout.jsx';
import { RequireAuth } from './auth.jsx';
import Home from './pages/Home.jsx';
import Search from './pages/Search.jsx';
import ListingPage from './pages/ListingPage.jsx';
import ReviewPage from './pages/ReviewPage.jsx';
import WriteReview from './pages/WriteReview.jsx';
import MyReviews from './pages/MyReviews.jsx';
import Login from './pages/Login.jsx';
import Register from './pages/Register.jsx';
import Moderation from './pages/Moderation.jsx';
import Policy from './pages/Policy.jsx';
import NotFound from './pages/NotFound.jsx';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Home />} />
        <Route path="search" element={<Search />} />
        <Route path="listings/:id" element={<ListingPage />} />
        <Route path="reviews/:id" element={<ReviewPage />} />
        <Route path="write" element={<RequireAuth><WriteReview /></RequireAuth>} />
        <Route path="me" element={<RequireAuth><MyReviews /></RequireAuth>} />
        <Route path="moderation" element={<RequireAuth role="Moderator"><Moderation /></RequireAuth>} />
        <Route path="login" element={<Login />} />
        <Route path="register" element={<Register />} />
        <Route path="policy" element={<Policy />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}
