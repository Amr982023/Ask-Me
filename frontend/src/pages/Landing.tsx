import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Landing() {
  const { user } = useAuth();

  return (
    <div className="max-w-3xl mx-auto px-4 py-16 sm:py-24 text-center">
      <span className="inline-block bg-brand-50 dark:bg-brand-950 text-brand-700 dark:text-brand-400 text-xs font-semibold px-3 py-1 rounded-full mb-6">
        Anonymous, honest, yours
      </span>
      <h1 className="text-4xl sm:text-5xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100 mb-4">
        Ask anything.<br />Answer on your terms.
      </h1>
      <p className="text-slate-600 dark:text-slate-400 text-lg mb-8 max-w-xl mx-auto">
        Get a public profile where anyone can ask you a question — anonymously or not.
        Answer the ones you like, share the best ones, and let the community vote up what matters.
      </p>

      <div className="flex items-center justify-center gap-3 mb-16">
        {user ? (
          <Link to={`/u/${user.username}`} className="btn-primary text-base">Go to my profile</Link>
        ) : (
          <>
            <Link to="/register" className="btn-primary text-base">Create your profile</Link>
            <Link to="/login" className="btn-secondary text-base">Log in</Link>
          </>
        )}
      </div>

      <div className="grid sm:grid-cols-3 gap-4 text-left">
        <div className="card p-5">
          <div className="w-9 h-9 rounded-lg bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-400 flex items-center justify-center font-bold mb-3">1</div>
          <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">Share your link</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400">Your profile lives at a simple, shareable URL.</p>
        </div>
        <div className="card p-5">
          <div className="w-9 h-9 rounded-lg bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-400 flex items-center justify-center font-bold mb-3">2</div>
          <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">Get questions</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400">Anonymous or named — you choose what's public.</p>
        </div>
        <div className="card p-5">
          <div className="w-9 h-9 rounded-lg bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-400 flex items-center justify-center font-bold mb-3">3</div>
          <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">Answer & share</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400">Add photos to your answers and share your favorites.</p>
        </div>
      </div>
    </div>
  );
}
