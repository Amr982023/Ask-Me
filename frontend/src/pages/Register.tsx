import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../api/client";
import GoogleSignInButton from "../components/GoogleSignInButton";
import FacebookLoginButton from "../components/FacebookLoginButton";

export default function Register() {
  const { register, loginWithGoogle, loginWithFacebook } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await register(username, email, password, displayName);
      // Account exists but can't log in yet - collect the emailed code next.
      navigate("/verify-email", { state: { email } });
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const withOAuthError = (fn: (token: string) => Promise<void>) => async (token: string) => {
    setError(null);
    try {
      await fn(token);
      navigate("/dashboard");
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="max-w-sm mx-auto px-4 py-16">
      <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 mb-6 text-center">Create your profile</h1>

      <div className="flex flex-col gap-2 mb-4">
        <GoogleSignInButton onToken={withOAuthError(loginWithGoogle)} />
        <FacebookLoginButton onToken={withOAuthError(loginWithFacebook)} />
      </div>
      {/* <div className="flex items-center gap-3 mb-4 text-xs text-slate-400 dark:text-slate-500">
        <div className="flex-1 h-px bg-slate-200 dark:bg-slate-700" /> or <div className="flex-1 h-px bg-slate-200 dark:bg-slate-700" />
      </div> */}

      <form onSubmit={onSubmit} className="card p-6 flex flex-col gap-4">
        {error && <p className="text-sm text-red-600 bg-red-50 dark:bg-red-950 rounded-lg px-3 py-2">{error}</p>}
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Username</label>
          <input className="input" required minLength={3} maxLength={30}
            pattern="[a-zA-Z0-9_]+" title="Letters, numbers and underscores only"
            value={username} onChange={(e) => setUsername(e.target.value)} />
          <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">Your profile: askme.app/u/{username || "username"}</p>
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Display name</label>
          <input className="input" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Email</label>
          <input className="input" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Password</label>
          <input className="input" type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} />
          <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">At least 8 characters.</p>
        </div>
        <button className="btn-primary" disabled={busy}>{busy ? "Creating..." : "Create account"}</button>
      </form>
      <p className="text-sm text-slate-500 dark:text-slate-400 text-center mt-4">
        Already have an account? <Link to="/login" className="text-brand-700 dark:text-brand-400 font-medium">Log in</Link>
      </p>
    </div>
  );
}
