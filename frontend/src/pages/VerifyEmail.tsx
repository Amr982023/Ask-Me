import { FormEvent, useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../api/client";
import { useCountdown } from "../hooks/useCountdown";

const RESEND_COOLDOWN_SECONDS = 60;

export default function VerifyEmail() {
  const { verifyEmail, resendVerification } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const initialEmail = (location.state as { email?: string } | null)?.email || "";

  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [resendBusy, setResendBusy] = useState(false);
  const [resent, setResent] = useState(false);
  const { secondsLeft, start, isActive } = useCountdown(RESEND_COOLDOWN_SECONDS);

  // A code was already sent to get here from Register - start the cooldown immediately.
  useEffect(() => { start(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await verifyEmail(email, code);
      navigate("/dashboard");
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
      await resendVerification(email);
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
      <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 mb-2 text-center">Verify your email</h1>
      <p className="text-sm text-slate-500 dark:text-slate-400 text-center mb-6">
        We sent a 6-digit code to your email. It's valid for 10 minutes — after that you'll need a new one.
      </p>
      <form onSubmit={onSubmit} className="card p-6 flex flex-col gap-4">
        {error && <p className="text-sm text-red-600 bg-red-50 dark:bg-red-950 rounded-lg px-3 py-2">{error}</p>}
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Email</label>
          <input className="input" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Verification code</label>
          <input className="input tracking-widest text-center text-lg" required maxLength={6} inputMode="numeric"
            value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))} />
        </div>
        <button className="btn-primary" disabled={busy || code.length !== 6}>
          {busy ? "Verifying..." : "Verify & continue"}
        </button>
      </form>
      <button
        onClick={resend}
        disabled={isActive || resendBusy}
        className="text-sm text-brand-700 dark:text-brand-400 font-medium text-center w-full mt-4 disabled:text-slate-400 dark:disabled:text-slate-600"
      >
        {resent ? "Code resent!" : isActive ? `Resend code in ${secondsLeft}s` : "Resend code"}
      </button>
    </div>
  );
}
