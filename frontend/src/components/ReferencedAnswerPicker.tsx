import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { MyAnswerSummary } from "../types";

export default function ReferencedAnswerPicker({ value, onChange }: { value: string | null; onChange: (id: string | null) => void }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<MyAnswerSummary[]>([]);
  const [open, setOpen] = useState(false);
  const [selectedLabel, setSelectedLabel] = useState<string | null>(null);

  useEffect(() => {
    const handler = setTimeout(async () => {
      const { data } = await api.get<MyAnswerSummary[]>("/me/answers", { params: { search: query || undefined } });
      setResults(data);
    }, 250);
    return () => clearTimeout(handler);
  }, [query]);

  const pick = (a: MyAnswerSummary) => {
    onChange(a.answerId);
    setSelectedLabel(a.questionContent);
    setOpen(false);
  };

  if (value && selectedLabel) {
    return (
      <div className="flex items-center gap-2 text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-2">
        <span className="text-slate-500 dark:text-slate-400">Referencing:</span>
        <span className="truncate flex-1">{selectedLabel}</span>
        <button type="button" onClick={() => { onChange(null); setSelectedLabel(null); }} className="text-red-500 dark:text-red-400 shrink-0">✕</button>
      </div>
    );
  }

  return (
    <div className="relative">
      <input
        className="input !py-1.5 text-xs"
        placeholder="Link a previous answer (optional)..."
        value={query}
        onFocus={() => setOpen(true)}
        onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
      />
      {open && results.length > 0 && (
        <div className="absolute z-20 mt-1 w-full card p-1.5 max-h-48 overflow-y-auto">
          {results.map((a) => (
            <button key={a.answerId} type="button" onClick={() => pick(a)}
              className="w-full text-left px-2.5 py-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-xs truncate">
              {a.questionContent}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
