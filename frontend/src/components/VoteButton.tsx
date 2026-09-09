import { useState } from "react";
import { api, getErrorMessage } from "../api/client";

interface Props {
  questionId: string;
  initialCount: number;
  initialHasVoted: boolean;
}

export default function VoteButton({ questionId, initialCount, initialHasVoted }: Props) {
  const [count, setCount] = useState(initialCount);
  const [hasVoted, setHasVoted] = useState(initialHasVoted);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Clicking again removes the vote - a toggle, not a one-way action.
  const toggleVote = async () => {
    if (busy) return;
    setBusy(true);
    setError(null);

    const wasVoted = hasVoted;
    // Optimistic update, rolled back if the server rejects.
    setCount((c) => (wasVoted ? c - 1 : c + 1));
    setHasVoted(!wasVoted);

    try {
      const { data } = wasVoted
        ? await api.delete(`/questions/${questionId}/vote`)
        : await api.post(`/questions/${questionId}/vote`);
      setCount(data.voteCount);
    } catch (err) {
      setCount((c) => (wasVoted ? c + 1 : c - 1));
      setHasVoted(wasVoted);
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex flex-col items-center gap-0.5 shrink-0">
      <button
        onClick={toggleVote}
        disabled={busy}
        title={hasVoted ? "Remove your upvote" : "Upvote"}
        className={`w-11 h-11 rounded-xl border flex items-center justify-center transition-colors
          ${hasVoted
            ? "bg-brand-600 border-brand-600 text-white"
            : "bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-700 text-slate-500 dark:text-slate-400 hover:border-brand-400 hover:text-brand-600 dark:hover:text-brand-400"}`}
      >
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <path d="M12 19V5M5 12l7-7 7 7" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      <span className="text-sm font-semibold text-slate-700 dark:text-slate-300">{count}</span>
      {error && <span className="text-[10px] text-red-500 dark:text-red-400 text-center max-w-[70px]">{error}</span>}
    </div>
  );
}
