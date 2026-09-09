import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { api, getErrorMessage } from "../api/client";
import Avatar from "../components/Avatar";
import { useCountdown } from "../hooks/useCountdown";

const RESEND_COOLDOWN_SECONDS = 60;

const SOCIAL_FIELDS: { key: keyof SocialLinksForm; label: string; placeholder: string }[] = [
  { key: "websiteUrl", label: "Website", placeholder: "yoursite.com" },
  { key: "twitterUrl", label: "X / Twitter", placeholder: "x.com/you" },
  { key: "facebookUrl", label: "Facebook", placeholder: "facebook.com/you" },
  { key: "githubUrl", label: "GitHub", placeholder: "github.com/you" },
  { key: "linkedinUrl", label: "LinkedIn", placeholder: "linkedin.com/in/you" },
  { key: "youtubeUrl", label: "YouTube", placeholder: "youtube.com/@you" },
  { key: "tiktokUrl", label: "TikTok", placeholder: "tiktok.com/@you" },
];

interface SocialLinksForm {
  websiteUrl: string; twitterUrl: string; facebookUrl: string;
  githubUrl: string; linkedinUrl: string; youtubeUrl: string; tiktokUrl: string;
}

function ChangePasswordCard({ hasPassword }: { hasPassword: boolean }) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [code, setCode] = useState("");
  const [codeSent, setCodeSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [sendBusy, setSendBusy] = useState(false);
  const [confirmBusy, setConfirmBusy] = useState(false);
  const { secondsLeft, start, isActive } = useCountdown(RESEND_COOLDOWN_SECONDS);

  const sendCode = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setSendBusy(true);
    try {
      await api.post("/auth/request-password-change-otp");
      setCodeSent(true);
      start();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSendBusy(false);
    }
  };

  const resend = async () => {
    if (isActive) return;
    setSendBusy(true);
    setError(null);
    try {
      await api.post("/auth/request-password-change-otp");
      start();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSendBusy(false);
    }
  };

  const confirm = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setConfirmBusy(true);
    try {
      await api.post("/auth/change-password", {
        currentPassword: hasPassword ? currentPassword : undefined,
        newPassword,
        code,
      });
      setSaved(true);
      setCurrentPassword("");
      setNewPassword("");
      setCode("");
      setCodeSent(false);
      setTimeout(() => setSaved(false), 3000);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setConfirmBusy(false);
    }
  };

  return (
    <div>
      <h2 className="text-lg font-bold text-slate-900 dark:text-slate-100 mb-3 text-center">
        {hasPassword ? "Change password" : "Set a password"}
      </h2>
      <form onSubmit={codeSent ? confirm : sendCode} className="card p-6 flex flex-col gap-4">
        {error && <p className="text-sm text-red-600 bg-red-50 dark:bg-red-950 rounded-lg px-3 py-2">{error}</p>}
        {!hasPassword && (
          <p className="text-xs text-slate-500 dark:text-slate-400">
            Your account currently signs in via Google/Facebook only. Set a password to also log in with your email.
          </p>
        )}

        {hasPassword && (
          <div>
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Current password</label>
            <input className="input" type="password" required disabled={codeSent}
              value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} />
          </div>
        )}
        <div>
          <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">New password</label>
          <input className="input" type="password" required minLength={8} disabled={codeSent}
            value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
        </div>

        {!codeSent ? (
          <button className="btn-primary" disabled={sendBusy || !newPassword || (hasPassword && !currentPassword)}>
            {sendBusy ? "Sending..." : "Send verification code"}
          </button>
        ) : (
          <>
            <div>
              <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Verification code</label>
              <input className="input tracking-widest text-center text-lg" required maxLength={6} inputMode="numeric"
                value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))} />
              <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">
                We emailed a code to confirm this change. It's valid for 10 minutes.
              </p>
            </div>
            <button className="btn-primary" disabled={confirmBusy || code.length !== 6}>
              {confirmBusy ? "Updating..." : hasPassword ? "Confirm password change" : "Confirm and set password"}
            </button>
            <button type="button" onClick={resend} disabled={isActive || sendBusy}
              className="text-sm text-brand-700 dark:text-brand-400 font-medium disabled:text-slate-400 dark:disabled:text-slate-600">
              {isActive ? `Resend code in ${secondsLeft}s` : "Resend code"}
            </button>
          </>
        )}
        {saved && <p className="text-sm text-green-600 text-center">Password updated!</p>}
      </form>
    </div>
  );
}

export default function EditProfile() {
  const { user, refreshMe } = useAuth();
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [bio, setBio] = useState("");
  const [aboutMe, setAboutMe] = useState("");
  const [social, setSocial] = useState<SocialLinksForm>({
    websiteUrl: "", twitterUrl: "", facebookUrl: "", githubUrl: "", linkedinUrl: "", youtubeUrl: "", tiktokUrl: "",
  });
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (user) {
      setUsername(user.username);
      setDisplayName(user.displayName);
      setBio(user.bio || "");
      setAboutMe(user.aboutMe || "");
      setPreview(user.profileImageUrl);
      setSocial({
        websiteUrl: user.socialLinks.websiteUrl || "",
        twitterUrl: user.socialLinks.twitterUrl || "",
        facebookUrl: user.socialLinks.facebookUrl || "",
        githubUrl: user.socialLinks.githubUrl || "",
        linkedinUrl: user.socialLinks.linkedinUrl || "",
        youtubeUrl: user.socialLinks.youtubeUrl || "",
        tiktokUrl: user.socialLinks.tiktokUrl || "",
      });
    }
  }, [user]);

  const onFileChange = (f: File | null) => {
    setFile(f);
    setPreview(f ? URL.createObjectURL(f) : user?.profileImageUrl || null);
  };

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (file) {
        const form = new FormData();
        form.append("file", file);
        await api.post("/users/me/profile-image", form, { headers: { "Content-Type": "multipart/form-data" } });
      }
      await api.put("/users/me", { username, displayName, bio, aboutMe, ...social });
      await refreshMe();
      setSaved(true);
      setTimeout(() => navigate(`/u/${username}`), 800);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  if (!user) return null;

  return (
    <div className="max-w-sm mx-auto px-4 py-12 flex flex-col gap-6">
      <div>
        <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100 mb-6 text-center">Edit profile</h1>
        <form onSubmit={onSubmit} className="card p-6 flex flex-col gap-4">
          {error && <p className="text-sm text-red-600 bg-red-50 dark:bg-red-950 rounded-lg px-3 py-2">{error}</p>}

          <div className="flex flex-col items-center gap-2">
            <Avatar src={preview} name={displayName || username} className="w-20 h-20 rounded-2xl text-2xl" />
            <input type="file" accept="image/jpeg,image/png,image/webp,image/gif"
              onChange={(e) => onFileChange(e.target.files?.[0] || null)}
              className="text-xs text-slate-500 dark:text-slate-400 file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:bg-brand-50 file:text-brand-700 file:text-xs file:font-medium" />
          </div>

          <div>
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Username</label>
            <input className="input" required minLength={3} maxLength={30} pattern="[a-zA-Z0-9_]+"
              value={username} onChange={(e) => setUsername(e.target.value)} />
            <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">Changing this changes your profile link.</p>
          </div>
          <div>
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Display name</label>
            <input className="input" required value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          </div>
          <div>
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">Short bio</label>
            <textarea className="input resize-none h-16" maxLength={300} value={bio} onChange={(e) => setBio(e.target.value)} />
          </div>
          <div>
            <label className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-1 block">About me</label>
            <textarea className="input resize-none h-28" maxLength={2000} placeholder="A longer introduction shown on your profile sidebar..."
              value={aboutMe} onChange={(e) => setAboutMe(e.target.value)} />
          </div>

          <div>
            <p className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">Social links</p>
            <div className="flex flex-col gap-2">
              {SOCIAL_FIELDS.map(({ key, label, placeholder }) => (
                <input key={key} className="input" placeholder={`${label} — ${placeholder}`}
                  value={social[key]} onChange={(e) => setSocial((s) => ({ ...s, [key]: e.target.value }))} />
              ))}
            </div>
          </div>

          <button className="btn-primary" disabled={busy}>{busy ? "Saving..." : "Save changes"}</button>
          {saved && <p className="text-sm text-green-600 text-center">Saved!</p>}
        </form>
      </div>

      <ChangePasswordCard hasPassword={user.hasPassword} />
    </div>
  );
}
