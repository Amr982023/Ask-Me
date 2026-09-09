import { FormEvent, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import type { QuestionDto } from "../types";
import ReferencedAnswerPicker from "./ReferencedAnswerPicker";

export default function AnswerForm({ question, onDone }: { question: QuestionDto; onDone: () => void }) {
  const [content, setContent] = useState(question.answer?.content || "");
  const [file, setFile] = useState<File | null>(null);
  const [youtubeUrl, setYoutubeUrl] = useState("");
  const [referencedAnswerId, setReferencedAnswerId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;
    setBusy(true);
    setError(null);
    try {
      let imageKey: string | undefined;
      if (file) {
        const form = new FormData();
        form.append("file", file);
        const { data } = await api.post("/questions/answer-image", form, {
          headers: { "Content-Type": "multipart/form-data" },
        });
        imageKey = data.imageKey;
      }
      await api.post(`/questions/${question.id}/answer`, {
        content: content.trim(), imageKey, youtubeUrl: youtubeUrl.trim() || undefined, referencedAnswerId,
      });
      onDone();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <form onSubmit={submit} className="mt-3 pl-4 border-l-2 border-brand-200 dark:border-brand-900 flex flex-col gap-2" onClick={(e) => e.stopPropagation()}>
      <textarea className="input resize-none h-20" placeholder="Write your answer..." maxLength={1000}
        value={content} onChange={(e) => setContent(e.target.value)} />
      <input className="input !py-1.5 text-xs" placeholder="YouTube video link (optional)"
        value={youtubeUrl} onChange={(e) => setYoutubeUrl(e.target.value)} />
      <ReferencedAnswerPicker value={referencedAnswerId} onChange={setReferencedAnswerId} />
      <input type="file" accept="image/jpeg,image/png,image/webp,image/gif"
        onChange={(e) => setFile(e.target.files?.[0] || null)}
        className="text-xs text-slate-500 dark:text-slate-400 file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:bg-brand-50 dark:file:bg-brand-950 file:text-brand-700 dark:file:text-brand-400 file:text-xs file:font-medium" />
      {error && <p className="text-xs text-red-600 dark:text-red-400">{error}</p>}
      <button className="btn-primary self-start !py-1.5 !px-3 text-sm" disabled={busy || !content.trim()}>
        {busy ? "Posting..." : question.answer ? "Update answer" : "Post answer"}
      </button>
    </form>
  );
}
