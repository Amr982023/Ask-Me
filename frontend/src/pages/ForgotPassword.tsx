import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function ForgotPassword() {
  const { forgotPassword } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    try {
      await forgotPassword(email);
      setSent(true);
      setTimeout(() => navigate("/reset-password", { state: { email } }), 1200);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-sm mx-auto px-4 py-16">
      <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 mb-2 text-center">Reset your password</h1>
      <p className="text-sm text-slate-500 dark:text-slate-400 text-center mb-6">
        Enter your account email and we'll send a reset code.
      </p>
      <form onSubmit={onSubmit} className="card p-6 flex flex-col gap-4">
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Email</label>
          <input className="input" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>
        <button className="btn-primary" disabled={busy || !email}>
          {busy ? "Sending..." : "Send reset code"}
        </button>
        {sent && <p className="text-sm text-green-600 dark:text-green-400 text-center">If that email exists, a code is on its way.</p>}
      </form>
    </div>
  );
}
