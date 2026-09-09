import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, getErrorMessage } from "../api/client";
import type { PagedResult, PublicProfile, QuestionDto, QuestionSort } from "../types";
import QuestionCard from "../components/QuestionCard";
import Avatar from "../components/Avatar";
import AvatarLightbox from "../components/AvatarLightbox";
import SocialLinksRow from "../components/SocialLinksRow";
import { useAuth } from "../context/AuthContext";

export default function Profile() {
  const { username = "" } = useParams();
  const { user } = useAuth();
  const isOwnProfile = user?.username === username.trim().toLowerCase();
  const [profile, setProfile] = useState<PublicProfile | null>(null);
  const [feed, setFeed] = useState<PagedResult<QuestionDto> | null>(null);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<QuestionSort>("Votes");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [lightboxOpen, setLightboxOpen] = useState(false);

  const [content, setContent] = useState("");
  const [isAnonymous, setIsAnonymous] = useState(true);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitBusy, setSubmitBusy] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const loadProfile = async () => {
    setLoading(true);
    setNotFound(false);
    try {
      const { data } = await api.get<PublicProfile>(`/users/${username}`);
      setProfile(data);
    } catch {
      setNotFound(true);
    } finally {
      setLoading(false);
    }
  };

  const loadFeed = async (p: number, s: QuestionSort, q: string) => {
    try {
      const { data } = await api.get<PagedResult<QuestionDto>>(`/users/${username}/questions`, {
        params: { page: p, pageSize: 10, sort: s, search: q || undefined },
      });
      setFeed(data);
    } catch {
      // Feed errors don't block the profile from rendering.
    }
  };

  useEffect(() => { loadProfile(); }, [username]);
  useEffect(() => { if (profile) loadFeed(page, sort, search); }, [profile, page, sort]);

  // Debounce search-as-you-type separately from sort/page changes.
  useEffect(() => {
    if (!profile) return;
    const handler = setTimeout(() => { setPage(1); loadFeed(1, sort, search); }, 300);
    return () => clearTimeout(handler);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const submitQuestion = async (e: FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;
    setSubmitBusy(true);
    setSubmitError(null);
    try {
      await api.post(`/users/${username}/questions`, { content: content.trim(), isAnonymous });
      setContent("");
      setSubmitted(true);
      setPage(1);
      await loadFeed(1, sort, search);
      setProfile((p) => (p ? { ...p, questionCount: p.questionCount + 1 } : p));
      setTimeout(() => setSubmitted(false), 3000);
    } catch (err) {
      setSubmitError(getErrorMessage(err));
    } finally {
      setSubmitBusy(false);
    }
  };

  if (loading) return <div className="max-w-5xl mx-auto px-4 py-16 text-center text-slate-400 dark:text-slate-500">Loading...</div>;
  if (notFound || !profile) return (
    <div className="max-w-5xl mx-auto px-4 py-16 text-center">
      <p className="text-slate-500 dark:text-slate-400">No profile found for <span className="font-mono">@{username}</span>.</p>
    </div>
  );

  return (
    <div className="max-w-5xl mx-auto px-4 py-8 sm:py-12 grid lg:grid-cols-[1fr_320px] gap-6 items-start">
      {/* Main column */}
      <div className="min-w-0 flex flex-col gap-6">
        <div className="card p-6 flex items-center gap-4">
          <button onClick={() => profile.profileImageUrl && setLightboxOpen(true)} className="shrink-0">
            <Avatar src={profile.profileImageUrl} name={profile.displayName} className="w-16 h-16 rounded-2xl text-2xl" />
          </button>
          <div className="min-w-0 flex-1">
            <div className="flex items-center justify-between gap-2">
              <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100 truncate">{profile.displayName}</h1>
              {isOwnProfile && (
                <Link to="/settings" className="btn-secondary !py-1.5 !px-3 text-sm shrink-0">
                  Edit profile
                </Link>
              )}
            </div>
            <p className="text-slate-500 dark:text-slate-400 text-sm">
              @{profile.username} · {profile.totalAnswers} Answers · {profile.totalVotes} likes
            </p>
            {profile.bio && <p className="text-slate-600 dark:text-slate-400 text-sm mt-1">{profile.bio}</p>}
          </div>
        </div>

        {/* Ask form */}
        <form onSubmit={submitQuestion} className="card p-5">
          <label className="text-sm font-semibold text-slate-800 dark:text-slate-200 mb-2 block">
            Ask {profile.displayName} something
          </label>
          <textarea
            className="input resize-none h-24"
            maxLength={1000}
            placeholder="Type your question..."
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
          <div className="flex items-center justify-between mt-3 flex-wrap gap-2">
            <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 cursor-pointer">
              <input type="checkbox" checked={isAnonymous} onChange={(e) => setIsAnonymous(e.target.checked)}
                className="w-4 h-4 rounded accent-brand-600" />
              Ask anonymously
            </label>
            <button className="btn-primary" disabled={submitBusy || !content.trim()}>
              {submitBusy ? "Sending..." : "Send question"}
            </button>
          </div>
          {submitError && <p className="text-sm text-red-600 dark:text-red-400 mt-2">{submitError}</p>}
          {submitted && <p className="text-sm text-green-600 dark:text-green-400 mt-2">Question sent!</p>}
        </form>

        {/* Feed controls */}
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <h2 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Public questions</h2>
          <div className="flex items-center gap-2">
            <input
              className="input !py-1.5 text-sm w-40"
              placeholder="Search answers..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <select
              className="input !py-1.5 text-sm w-auto"
              value={sort}
              onChange={(e) => { setSort(e.target.value as QuestionSort); setPage(1); }}
            >
              <option value="Votes">Most voted</option>
              <option value="Recent">Most recent</option>
            </select>
          </div>
        </div>

        <div className="flex flex-col gap-3">
          {feed && feed.items.length === 0 && (
            <p className="text-slate-400 dark:text-slate-500 text-sm text-center py-8">No public questions yet.</p>
          )}
          {feed?.items.map((q) => (
            <QuestionCard
              key={q.id}
              question={q}
              linkToDetail={!isOwnProfile}
              showOwnerControls={isOwnProfile}
              onOwnerChange={() => loadFeed(page, sort, search)}
            />
          ))}
        </div>

        {feed && feed.totalCount > feed.pageSize && (
          <div className="flex items-center justify-center gap-3">
            <button className="btn-secondary text-sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
            <span className="text-sm text-slate-500 dark:text-slate-400">Page {feed.page}</span>
            <button className="btn-secondary text-sm" disabled={!feed.hasMore} onClick={() => setPage((p) => p + 1)}>Next</button>
          </div>
        )}
      </div>

      {/* Sidebar */}
      <div className="flex flex-col gap-4">
        {profile.aboutMe && (
          <div className="card p-5">
            <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-2">About me</h3>
            <p className="text-sm text-slate-600 dark:text-slate-400 whitespace-pre-wrap">{profile.aboutMe}</p>
          </div>
        )}
        <div className="card p-5">
          <SocialLinksRow links={profile.socialLinks} />
        </div>
      </div>

      {lightboxOpen && profile.profileImageUrl && (
        <AvatarLightbox src={profile.profileImageUrl} name={profile.displayName} onClose={() => setLightboxOpen(false)} />
      )}
    </div>
  );
}
