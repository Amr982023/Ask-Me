import { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  if (loading) return <div className="text-center py-16 text-slate-400 dark:text-slate-500">Loading...</div>;
  if (!user) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

// Admin functionality is never reachable through the normal user interface -
// this guard keeps /admin unreachable for anyone without the Admin role,
// even if they guess the URL. The backend enforces this independently too.
export function AdminRoute({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  if (loading) return <div className="text-center py-16 text-slate-400 dark:text-slate-500">Loading...</div>;
  if (!user || user.role !== "Admin") return <Navigate to="/" replace />;
  return <>{children}</>;
}
