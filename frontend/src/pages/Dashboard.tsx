import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";
import type { PagedResult, QuestionDto, QuestionSort } from "../types";
import OwnerControls from "../components/OwnerControls";

function OwnerQuestionRow({ question, onChange }: { question: QuestionDto; onChange: () => void }) {
  return (
    <div className="card p-4">
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400 mb-1">
          {question.isAnonymous ? (
            <span className="bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-full font-medium">Anonymous</span>
          ) : (
            <span className="bg-brand-50 dark:bg-brand-950 text-brand-700 dark:text-brand-400 px-2 py-0.5 rounded-full font-medium">
              {question.authorDisplayName || question.authorUsername}
            </span>
          )}
          <span>· {question.voteCount} votes</span>
          {!question.isVisible && <span className="bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded-full font-medium">Hidden</span>}
        </div>
        <p className="text-slate-800 dark:text-slate-200 break-words">{question.content}</p>
        {question.answer && (
          <div className="mt-2 pl-3 border-l-2 border-slate-200 dark:border-slate-700 text-sm text-slate-600 dark:text-slate-400">
            {question.answer.content}
            {question.answer.imageUrl && (
              <img src={question.answer.imageUrl} className="mt-2 rounded-lg max-h-40 border border-slate-200 dark:border-slate-700" />
            )}
          </div>
        )}
      </div>

      <OwnerControls question={question} onChange={onChange} />
    </div>
  );
}

export default function Dashboard() {
  const { user } = useAuth();
  const [feed, setFeed] = useState<PagedResult<QuestionDto> | null>(null);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<QuestionSort>("Votes");
  const [loading, setLoading] = useState(true);

  const load = async (p: number, s: QuestionSort) => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<QuestionDto>>("/me/questions", { params: { page: p, pageSize: 15, sort: s } });
      setFeed(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(page, sort); }, [page, sort]);

  if (!user) return null;

  return (
    <div className="max-w-2xl mx-auto px-4 py-8 sm:py-12">
      <div className="flex items-center justify-between mb-6 flex-wrap gap-2">
        <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">Your questions</h1>
        <div className="flex items-center gap-3">
          <select className="input !py-1.5 text-sm w-auto" value={sort}
            onChange={(e) => { setSort(e.target.value as QuestionSort); setPage(1); }}>
            <option value="Votes">Most voted</option>
            <option value="Recent">Most recent</option>
          </select>
          <Link to="/settings" className="text-sm text-brand-700 dark:text-brand-400 font-medium">Edit profile</Link>
        </div>
      </div>

      {loading && <p className="text-center text-slate-400 dark:text-slate-500 py-8">Loading...</p>}
      {!loading && feed?.items.length === 0 && (
        <p className="text-center text-slate-400 dark:text-slate-500 py-8">No questions yet — share your profile link to get started!</p>
      )}

      <div className="flex flex-col gap-3">
        {feed?.items.map((q) => (
          <OwnerQuestionRow key={q.id} question={q} onChange={() => load(page, sort)} />
        ))}
      </div>

      {feed && feed.totalCount > feed.pageSize && (
        <div className="flex items-center justify-center gap-3 mt-6">
          <button className="btn-secondary text-sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
          <span className="text-sm text-slate-500 dark:text-slate-400">Page {feed.page}</span>
          <button className="btn-secondary text-sm" disabled={!feed.hasMore} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
      )}
    </div>
  );
}
