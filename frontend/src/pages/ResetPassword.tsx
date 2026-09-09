import { FormEvent, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../api/client";
import { useCountdown } from "../hooks/useCountdown";

const RESEND_COOLDOWN_SECONDS = 60;

export default function ResetPassword() {
  const { resetPassword, forgotPassword } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const initialEmail = (location.state as { email?: string } | null)?.email || "";

  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [resendBusy, setResendBusy] = useState(false);
  const [resent, setResent] = useState(false);
  const { secondsLeft, start, isActive } = useCountdown(RESEND_COOLDOWN_SECONDS);

  // A code was already sent to get here from Forgot Password - start the cooldown immediately.
  useEffect(() => { if (initialEmail) start(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await resetPassword(email, code, newPassword);
      navigate("/login");
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const resend = async () => {
    if (!email || isActive) return;
    setResendBusy(true);
    setError(null);
    try {
      await forgotPassword(email);
      setResent(true);
      start();
      setTimeout(() => setResent(false), 4000);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setResendBusy(false);
    }
  };

  return (
    <div className="max-w-sm mx-auto px-4 py-16">
      <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 mb-2 text-center">Enter your new password</h1>
      <p className="text-sm text-slate-500 dark:text-slate-400 text-center mb-6">
        Your reset code is valid for 10 minutes.
      </p>
      <form onSubmit={onSubmit} className="card p-6 flex flex-col gap-4">
        {error && <p className="text-sm text-red-600 bg-red-50 dark:bg-red-950 rounded-lg px-3 py-2">{error}</p>}
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Email</label>
          <input className="input" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Reset code</label>
          <input className="input tracking-widest text-center text-lg" required maxLength={6} inputMode="numeric"
            value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))} />
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">New password</label>
          <input className="input" type="password" required minLength={8}
            value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
        </div>
        <button className="btn-primary" disabled={busy || code.length !== 6}>
          {busy ? "Updating..." : "Update password"}
        </button>
      </form>
      <button
        onClick={resend}
        disabled={isActive || resendBusy || !email}
        className="text-sm text-brand-700 dark:text-brand-400 font-medium text-center w-full mt-4 disabled:text-slate-400 dark:disabled:text-slate-600"
      >
        {resent ? "Code resent!" : isActive ? `Resend code in ${secondsLeft}s` : "Resend code"}
      </button>
    </div>
  );
}
