import { useState } from "react";
import { Link } from "react-router-dom";
import type { QuestionDto } from "../types";
import { api, getErrorMessage } from "../api/client";
import VoteButton from "./VoteButton";
import OwnerControls from "./OwnerControls";

function formatDateTime(iso: string) {
  const d = new Date(iso);
  const datePart = d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" });
  const timePart = d.toLocaleTimeString("en-US", { hour: "numeric", minute: "2-digit", hour12: true });
  return `${datePart} at ${timePart}`;
}

function FollowUpSection({ questionId, initial }: { questionId: string; initial: QuestionDto["followUps"] }) {
  const [followUps, setFollowUps] = useState(initial);
  const [open, setOpen] = useState(false);
  const [content, setContent] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!content.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const { data } = await api.post(`/questions/${questionId}/follow-up`, { content: content.trim() });
      setFollowUps((f) => [...f, data]);
      setContent("");
      setOpen(false);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mt-3" onClick={(e) => e.stopPropagation()}>
      {followUps.length > 0 && (
        <div className="flex flex-col gap-2 mb-2">
          {followUps.map((f) => (
            <div key={f.id} className="pl-4 border-l-2 border-slate-200 dark:border-slate-700 text-sm text-slate-600 dark:text-slate-400">
              {f.content}
              <span className="block text-xs text-slate-400 dark:text-slate-500 mt-0.5">{formatDateTime(f.createdAt)}</span>
            </div>
          ))}
        </div>
      )}

      {open ? (
        <div className="flex flex-col gap-2">
          <textarea className="input resize-none h-16 text-sm" maxLength={500} placeholder="Ask a follow-up..."
            value={content} onChange={(e) => setContent(e.target.value)} />
          {error && <p className="text-xs text-red-600 dark:text-red-400">{error}</p>}
          <div className="flex gap-2">
            <button onClick={submit} disabled={busy || !content.trim()} className="btn-primary !py-1.5 !px-3 text-xs">
              {busy ? "Posting..." : "Post follow-up"}
            </button>
            <button onClick={() => setOpen(false)} className="btn-secondary !py-1.5 !px-3 text-xs">Cancel</button>
          </div>
        </div>
      ) : (
        <button onClick={() => setOpen(true)} className="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400 hover:text-brand-700 dark:hover:text-brand-400 font-medium">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M21 11.5a8.38 8.38 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.38 8.38 0 01-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 01-.9-3.8 8.5 8.5 0 014.7-7.6 8.38 8.38 0 013.8-.9h.5a8.48 8.48 0 018 8v.5z" />
          </svg>
          Follow up
        </button>
      )}
    </div>
  );
}

interface Props {
  question: QuestionDto;
  /** Wraps the card in a Link to /q/{id} - set false on the detail page itself. */
  linkToDetail?: boolean;
  /** Shows Answer/Hide/Unhide/Delete controls - only pass true for the question's own recipient. */
  showOwnerControls?: boolean;
  /** Called after an owner action (answer/hide/unhide/delete) succeeds, so the parent can reload. */
  onOwnerChange?: () => void;
}

export default function QuestionCard({ question, linkToDetail = true, showOwnerControls = false, onOwnerChange }: Props) {
  // Owner-controlled cards are never wrapped in an outer Link (buttons/forms
  // inside an <a> would be broken/invalid HTML either way), so interactive
  // content (video, referenced-answer link, follow-ups) is safe to render
  // whenever there's no outer link - which is both the detail page AND any
  // card showing owner controls.
  const interactive = !linkToDetail || showOwnerControls;

  const header = (
    <div className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400 mb-1.5 flex-wrap">
      {question.isAnonymous ? (
        <span className="inline-flex items-center gap-1 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-2 py-0.5 rounded-full font-medium">
          Anonymous
        </span>
      ) : (
        <span className="inline-flex items-center gap-1 bg-brand-50 dark:bg-brand-950 text-brand-700 dark:text-brand-400 px-2 py-0.5 rounded-full font-medium">
          {question.authorDisplayName || question.authorUsername}
        </span>
      )}
      <span>·</span>
      <span>{formatDateTime(question.createdAt)}</span>
      {!question.isVisible && (
        <span className="bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded-full font-medium">Hidden</span>
      )}
      {!interactive && question.answer?.youtubeEmbedUrl && (
        <span className="text-slate-400 dark:text-slate-500">▶ video</span>
      )}
    </div>
  );

  const questionAndAnswer = (
    <>
      {header}
      <p className="text-slate-800 dark:text-slate-200 whitespace-pre-wrap break-words">{question.content}</p>

      {question.answer && (
        <div className="mt-3 pl-4 border-l-2 border-brand-200 dark:border-brand-900">
          <p className="text-sm font-semibold text-brand-700 dark:text-brand-400 mb-1">Answer</p>
          <p className="text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap break-words">{question.answer.content}</p>

          {question.answer.imageUrl && (
            <img
              src={question.answer.imageUrl}
              alt="Answer attachment"
              className="mt-2 rounded-xl max-h-64 object-cover border border-slate-200 dark:border-slate-700"
            />
          )}

          {interactive && question.answer.youtubeEmbedUrl && (
            <div className="mt-3 rounded-xl overflow-hidden aspect-video">
              <iframe
                src={question.answer.youtubeEmbedUrl}
                className="w-full h-full"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          )}

          {interactive && question.answer.referencedAnswer && (
            <Link
              to={`/q/${question.answer.referencedAnswer.questionId}`}
              className="mt-3 block rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 p-3 hover:bg-slate-100 dark:hover:bg-slate-700"
              onClick={(e) => e.stopPropagation()}
            >
              <p className="text-xs text-slate-400 dark:text-slate-500 mb-1">Referenced answer</p>
              <p className="text-sm text-slate-600 dark:text-slate-400 truncate">{question.answer.referencedAnswer.questionContent}</p>
            </Link>
          )}
        </div>
      )}
    </>
  );

  return (
    <div className="card p-4 flex gap-4">
      <VoteButton questionId={question.id} initialCount={question.voteCount} initialHasVoted={question.hasVoted} />
      <div className="flex-1 min-w-0">
        {linkToDetail && !showOwnerControls ? (
          <Link to={`/q/${question.id}`} className="block hover:opacity-90">{questionAndAnswer}</Link>
        ) : (
          questionAndAnswer
        )}
        {interactive && question.answer && (
          <FollowUpSection questionId={question.id} initial={question.followUps} />
        )}
        {showOwnerControls && onOwnerChange && (
          <OwnerControls question={question} onChange={onOwnerChange} />
        )}
      </div>
    </div>
  );
}
