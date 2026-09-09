import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "../api/client";
import type { QuestionDto } from "../types";
import QuestionCard from "../components/QuestionCard";

export default function QuestionDetail() {
  const { id } = useParams();
  const [question, setQuestion] = useState<QuestionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<QuestionDto>(`/questions/${id}`);
        setQuestion(data);
      } catch {
        setNotFound(true);
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  const share = async () => {
    // The page's own URL - always a valid, working link straight into the
    // app. Trade-off: social platforms (Facebook/WhatsApp/etc.) can't run
    // JS, so a shared link like this won't show a title/description/image
    // preview card - only the raw URL. That's the backend's /share/q/{id}
    // page's job (still in the codebase, just not used here) if you want
    // rich previews back later.
    const url = window.location.href;
    if (navigator.share) {
      try { await navigator.share({ title: "Ask Me", url }); return; } catch { /* user cancelled */ }
    }
    await navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (loading) return <div className="max-w-2xl mx-auto px-4 py-16 text-center text-slate-400 dark:text-slate-500">Loading...</div>;
  if (notFound || !question) return (
    <div className="max-w-2xl mx-auto px-4 py-16 text-center text-slate-500 dark:text-slate-400">
      This question isn't available — it may have been hidden or removed.
    </div>
  );

  return (
    <div className="max-w-2xl mx-auto px-4 py-8 sm:py-12">
      <QuestionCard question={question} linkToDetail={false} />
      <div className="flex justify-center mt-4">
        <button onClick={share} className="btn-secondary text-sm flex items-center gap-2">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="18" cy="5" r="3" /><circle cx="6" cy="12" r="3" /><circle cx="18" cy="19" r="3" />
            <path d="M8.6 13.5l6.8 3.9M15.4 6.6L8.6 10.5" />
          </svg>
          {copied ? "Link copied!" : "Share this question"}
        </button>
      </div>
    </div>
  );
}
