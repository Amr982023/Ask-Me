import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { AdminQuestionDto, AdminUserDto, BlockedIpDto, PagedResult, PlatformStatsDto, SecurityMetadataDto } from "../types";

function SecurityPanel({ questionId, onClose }: { questionId: string; onClose: () => void }) {
  const [data, setData] = useState<SecurityMetadataDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<SecurityMetadataDto>(`/admin/questions/${questionId}/security`);
        setData(data);
      } catch {
        setError("Unable to load security metadata.");
      }
    })();
  }, [questionId]);

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50" onClick={onClose}>
      <div className="card p-5 max-w-sm w-full" onClick={(e) => e.stopPropagation()}>
        <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-1">Security Investigation</h3>
        <p className="text-xs text-slate-400 dark:text-slate-500 mb-4">This access has been logged to the audit trail.</p>
        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
        {data && (
          <dl className="text-sm space-y-2">
            <div className="flex justify-between"><dt className="text-slate-500 dark:text-slate-400">Submitted</dt><dd>{new Date(data.submittedAt).toLocaleString()}</dd></div>
            <div className="flex justify-between"><dt className="text-slate-500 dark:text-slate-400">Source IP</dt><dd className="font-mono">{data.sourceIp}</dd></div>
            <div className="flex justify-between"><dt className="text-slate-500 dark:text-slate-400">Authenticated</dt><dd>{data.authenticatedUserId ? "Yes" : "No"}</dd></div>
            <div className="flex justify-between gap-3"><dt className="text-slate-500 dark:text-slate-400 shrink-0">User Agent</dt><dd className="text-right break-all text-xs">{data.userAgent || "—"}</dd></div>
          </dl>
        )}
        <button className="btn-secondary w-full mt-4 text-sm" onClick={onClose}>Close</button>
      </div>
    </div>
  );
}

function QuestionsTab() {
  const [feed, setFeed] = useState<PagedResult<AdminQuestionDto> | null>(null);
  const [page, setPage] = useState(1);
  const [investigating, setInvestigating] = useState<string | null>(null);

  const load = async (p: number) => {
    const { data } = await api.get<PagedResult<AdminQuestionDto>>("/admin/questions", { params: { page: p, pageSize: 15 } });
    setFeed(data);
  };

  useEffect(() => { load(page); }, [page]);

  const setVisibility = async (id: string, isVisible: boolean) => {
    await api.patch(`/admin/questions/${id}/visibility`, null, { params: { isVisible } });
    load(page);
  };

  const del = async (id: string) => {
    await api.delete(`/admin/questions/${id}`);
    load(page);
  };

  return (
    <div>
      <div className="flex flex-col gap-2">
        {feed?.items.map((q) => (
          <div key={q.id} className="card p-4">
            <div className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400 mb-1">
              <span className="font-medium text-slate-700 dark:text-slate-300">@{q.targetUsername}</span>
              <span>· {q.voteCount} votes</span>
              {q.isAnonymous && <span className="bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-full">Anonymous</span>}
              {!q.isVisible && <span className="bg-amber-100 dark:bg-amber-950 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded-full">Hidden</span>}
              {q.isDeleted && <span className="bg-red-100 dark:bg-red-950 text-red-700 dark:text-red-400 px-2 py-0.5 rounded-full">Deleted</span>}
            </div>
            <p className="text-sm text-slate-800 dark:text-slate-200 break-words mb-2">{q.content}</p>
            <div className="flex flex-wrap gap-2">
              {q.isVisible ? (
                <button className="btn-secondary !py-1.5 !px-3 text-xs" onClick={() => setVisibility(q.id, false)}>Hide</button>
              ) : (
                <button className="btn-secondary !py-1.5 !px-3 text-xs" onClick={() => setVisibility(q.id, true)}>Unhide</button>
              )}
              <button className="btn-danger" onClick={() => del(q.id)}>Delete</button>
              {q.isAnonymous && (
                <button className="btn-secondary !py-1.5 !px-3 text-xs" onClick={() => setInvestigating(q.id)}>
                  Investigate
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {feed && feed.totalCount > feed.pageSize && (
        <div className="flex items-center justify-center gap-3 mt-6">
          <button className="btn-secondary text-sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</button>
          <span className="text-sm text-slate-500 dark:text-slate-400">Page {feed.page}</span>
          <button className="btn-secondary text-sm" disabled={!feed.hasMore} onClick={() => setPage((p) => p + 1)}>Next</button>
        </div>
      )}

      {investigating && <SecurityPanel questionId={investigating} onClose={() => setInvestigating(null)} />}
    </div>
  );
}

function UsersTab() {
  const [users, setUsers] = useState<AdminUserDto[]>([]);
  const [search, setSearch] = useState("");
  const [blockPrompt, setBlockPrompt] = useState<{ ip: string; username: string } | null>(null);

  const load = async (q: string) => {
    const { data } = await api.get<AdminUserDto[]>("/admin/users", { params: { search: q || undefined } });
    setUsers(data);
  };

  useEffect(() => { load(search); }, []);
  useEffect(() => {
    const handler = setTimeout(() => load(search), 300);
    return () => clearTimeout(handler);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  const deleteUser = async (id: string, username: string) => {
    if (!confirm(`Delete @${username}? This removes their account and all questions sent to them.`)) return;
    await api.delete(`/admin/users/${id}`);
    load(search);
  };

  const blockIp = async (ip: string, reason: string) => {
    await api.post("/admin/blocked-ips", { ipAddress: ip, reason: reason || undefined });
    setBlockPrompt(null);
  };

  return (
    <div>
      <input
        className="input mb-4 max-w-xs"
        placeholder="Search by username or email..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <div className="flex flex-col gap-2">
        {users.map((u) => (
          <div key={u.id} className="card p-4 flex items-center justify-between gap-3 flex-wrap">
            <div className="min-w-0">
              <p className="font-medium text-slate-800 dark:text-slate-200">{u.displayName} <span className="text-slate-400 dark:text-slate-500 font-normal">@{u.username}</span></p>
              <p className="text-xs text-slate-500 dark:text-slate-400">{u.email} · {u.questionCount} questions · {u.role}</p>
              {u.registrationIp && <p className="text-xs text-slate-400 dark:text-slate-500 font-mono">Registered from {u.registrationIp}</p>}
            </div>
            <div className="flex gap-2 shrink-0">
              {u.registrationIp && (
                <button className="btn-secondary !py-1.5 !px-3 text-xs"
                  onClick={() => setBlockPrompt({ ip: u.registrationIp!, username: u.username })}>
                  Block IP
                </button>
              )}
              {u.role !== "Admin" && (
                <button className="btn-danger" onClick={() => deleteUser(u.id, u.username)}>Delete user</button>
              )}
            </div>
          </div>
        ))}
      </div>

      {blockPrompt && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50" onClick={() => setBlockPrompt(null)}>
          <div className="card p-5 max-w-sm w-full" onClick={(e) => e.stopPropagation()}>
            <h3 className="font-semibold text-slate-900 dark:text-slate-100 mb-3">Block {blockPrompt.ip}?</h3>
            <p className="text-xs text-slate-500 dark:text-slate-400 mb-3">This stops {blockPrompt.username}'s network from creating new accounts.</p>
            <BlockIpForm ip={blockPrompt.ip} onSubmit={blockIp} onCancel={() => setBlockPrompt(null)} />
          </div>
        </div>
      )}
    </div>
  );
}

function BlockIpForm({ ip, onSubmit, onCancel }: { ip: string; onSubmit: (ip: string, reason: string) => void; onCancel: () => void }) {
  const [reason, setReason] = useState("");
  return (
    <div className="flex flex-col gap-3">
      <input className="input text-sm" placeholder="Reason (optional)" value={reason} onChange={(e) => setReason(e.target.value)} />
      <div className="flex gap-2">
        <button className="btn-primary flex-1" onClick={() => onSubmit(ip, reason)}>Block</button>
        <button className="btn-secondary flex-1" onClick={onCancel}>Cancel</button>
      </div>
    </div>
  );
}

function BlockedIpsTab() {
  const [ips, setIps] = useState<BlockedIpDto[]>([]);
  const [newIp, setNewIp] = useState("");
  const [reason, setReason] = useState("");

  const load = async () => {
    const { data } = await api.get<BlockedIpDto[]>("/admin/blocked-ips");
    setIps(data);
  };

  useEffect(() => { load(); }, []);

  const add = async () => {
    if (!newIp.trim()) return;
    await api.post("/admin/blocked-ips", { ipAddress: newIp.trim(), reason: reason || undefined });
    setNewIp("");
    setReason("");
    load();
  };

  const remove = async (id: string) => {
    await api.delete(`/admin/blocked-ips/${id}`);
    load();
  };

  return (
    <div>
      <div className="card p-4 mb-4 flex flex-col sm:flex-row gap-2">
        <input className="input" placeholder="IP address" value={newIp} onChange={(e) => setNewIp(e.target.value)} />
        <input className="input" placeholder="Reason (optional)" value={reason} onChange={(e) => setReason(e.target.value)} />
        <button className="btn-primary shrink-0" onClick={add} disabled={!newIp.trim()}>Block</button>
      </div>
      <div className="flex flex-col gap-2">
        {ips.map((b) => (
          <div key={b.id} className="card p-4 flex items-center justify-between gap-3">
            <div>
              <p className="font-mono text-sm text-slate-800 dark:text-slate-200">{b.ipAddress}</p>
              {b.reason && <p className="text-xs text-slate-500 dark:text-slate-400">{b.reason}</p>}
            </div>
            <button className="btn-secondary !py-1.5 !px-3 text-xs" onClick={() => remove(b.id)}>Unblock</button>
          </div>
        ))}
        {ips.length === 0 && <p className="text-slate-400 dark:text-slate-500 text-sm text-center py-8">No blocked IPs.</p>}
      </div>
    </div>
  );
}

export default function Admin() {
  const [stats, setStats] = useState<PlatformStatsDto | null>(null);
  const [tab, setTab] = useState<"questions" | "users" | "blocked-ips">("questions");

  useEffect(() => {
    api.get<PlatformStatsDto>("/admin/stats").then(({ data }) => setStats(data));
  }, []);

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 sm:py-12">
      <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100 mb-6">Admin Dashboard</h1>

      {stats && (
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-3 mb-8">
          {[
            ["Users", stats.totalUsers],
            ["Questions", stats.totalQuestions],
            ["Answers", stats.totalAnswers],
            ["Hidden", stats.hiddenQuestions],
            ["Last 7d", stats.questionsLast7Days],
          ].map(([label, value]) => (
            <div key={label as string} className="card p-3 text-center">
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100">{value}</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">{label}</p>
            </div>
          ))}
        </div>
      )}

      <div className="flex gap-1 mb-4 border-b border-slate-200 dark:border-slate-800">
        {(["questions", "users", "blocked-ips"] as const).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
              ${tab === t ? "border-brand-600 text-brand-700 dark:text-brand-400" : "border-transparent text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"}`}
          >
            {t === "questions" ? "Questions" : t === "users" ? "Users" : "Blocked IPs"}
          </button>
        ))}
      </div>

      {tab === "questions" && <QuestionsTab />}
      {tab === "users" && <UsersTab />}
      {tab === "blocked-ips" && <BlockedIpsTab />}
    </div>
  );
}
