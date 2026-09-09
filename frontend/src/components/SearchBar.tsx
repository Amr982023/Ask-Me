import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import type { SearchResult } from "../types";

export default function SearchBar() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResult[]>([]);
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = setTimeout(async () => {
      if (query.trim().length < 2) {
        setResults([]);
        return;
      }
      try {
        const { data } = await api.get<SearchResult[]>("/users/search", { params: { query } });
        setResults(data);
      } catch {
        setResults([]);
      }
    }, 250); // debounce so we don't fire a request per keystroke

    return () => clearTimeout(handler);
  }, [query]);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  const go = (username: string) => {
    setOpen(false);
    setQuery("");
    navigate(`/u/${username}`);
  };

  return (
    <div ref={containerRef} className="relative w-full max-w-xs">
      <input
        className="input !py-2 text-sm"
        placeholder="Search @username..."
        value={query}
        onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
        onFocus={() => setOpen(true)}
      />
      {open && results.length > 0 && (
        <div className="absolute mt-1 w-full card p-1.5 z-50 max-h-72 overflow-y-auto">
          {results.map((r) => (
            <button
              key={r.username}
              onClick={() => go(r.username)}
              className="w-full flex items-center gap-2 px-2.5 py-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-left"
            >
              <div className="w-8 h-8 rounded-lg bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-400 flex items-center justify-center text-sm font-bold overflow-hidden shrink-0">
                {r.profileImageUrl ? <img src={r.profileImageUrl} className="w-full h-full object-cover" /> : r.displayName.charAt(0).toUpperCase()}
              </div>
              <div className="min-w-0">
                <p className="text-sm font-medium text-slate-800 dark:text-slate-200 truncate">{r.displayName}</p>
                <p className="text-xs text-slate-400 dark:text-slate-500 truncate">@{r.username}</p>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
