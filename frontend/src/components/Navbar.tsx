import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { useTheme } from "../context/ThemeContext";
import SearchBar from "./SearchBar";

function ThemeToggle() {
  const { isDark, toggle } = useTheme();
  return (
    <button
      onClick={toggle}
      title={isDark ? "Switch to light mode" : "Switch to dark mode"}
      className="w-9 h-9 rounded-lg flex items-center justify-center text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800 shrink-0"
    >
      {isDark ? (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="12" cy="12" r="5" />
          <path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" strokeLinecap="round" />
        </svg>
      ) : (
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M21 12.8A9 9 0 1111.2 3 7 7 0 0021 12.8z" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      )}
    </button>
  );
}

const linkClass = "text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-brand-700 dark:hover:text-brand-400 px-3 py-2";
const mobileLinkClass = "block w-full text-left text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-brand-700 dark:hover:text-brand-400 px-4 py-3 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800";

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);

  const closeMenu = () => setMenuOpen(false);

  return (
    <header className="sticky top-0 z-40 bg-white/80 dark:bg-slate-950/80 backdrop-blur border-b border-slate-200 dark:border-slate-800">
      <div className="max-w-4xl mx-auto px-4 h-16 flex items-center gap-3 justify-between">
        <Link to="/" className="flex items-center gap-2 font-extrabold text-xl text-brand-700 dark:text-brand-400 shrink-0">
          <span className="w-8 h-8 rounded-xl bg-brand-600 text-white flex items-center justify-center text-sm">?</span>
          <span className="hidden sm:inline">Ask Me</span>
        </Link>

        <div className="flex-1 max-w-xs hidden sm:block">
          <SearchBar />
        </div>

        {/* Desktop nav - unchanged, every link inline */}
        <nav className="hidden sm:flex items-center gap-2 sm:gap-3 shrink-0">
          {user ? (
            <>
              <Link to="/dashboard" className={linkClass}>Dashboard</Link>
              <Link to={`/u/${user.username}`} className={linkClass}>My Profile</Link>
              {user.role === "Admin" && <Link to="/admin" className={linkClass}>Admin</Link>}
              <ThemeToggle />
              <button onClick={() => { logout(); navigate("/"); }} className="btn-secondary !px-3 !py-2 text-sm">
                Log out
              </button>
            </>
          ) : (
            <>
              <ThemeToggle />
              <Link to="/login" className={linkClass}>Log in</Link>
              <Link to="/register" className="btn-primary !px-4 !py-2 text-sm">Sign up</Link>
            </>
          )}
        </nav>

        {/* Mobile: theme toggle always visible, everything else behind a menu button */}
        <div className="flex sm:hidden items-center gap-1 shrink-0">
          <ThemeToggle />
          {!user && (
            <Link to="/register" className="btn-primary !px-3 !py-2 text-sm">Sign up</Link>
          )}
          <button
            onClick={() => setMenuOpen((o) => !o)}
            aria-label="Menu"
            className="w-9 h-9 rounded-lg flex items-center justify-center text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            {menuOpen ? (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M18 6L6 18M6 6l12 12" strokeLinecap="round" /></svg>
            ) : (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M4 6h16M4 12h16M4 18h16" strokeLinecap="round" /></svg>
            )}
          </button>
        </div>
      </div>

      <div className="sm:hidden px-4 pb-3">
        <SearchBar />
      </div>

      {menuOpen && (
        <div className="sm:hidden border-t border-slate-200 dark:border-slate-800 px-2 py-2 flex flex-col gap-0.5">
          {user ? (
            <>
              <Link to="/dashboard" className={mobileLinkClass} onClick={closeMenu}>Dashboard</Link>
              <Link to={`/u/${user.username}`} className={mobileLinkClass} onClick={closeMenu}>My Profile</Link>
              {user.role === "Admin" && <Link to="/admin" className={mobileLinkClass} onClick={closeMenu}>Admin</Link>}
              <button
                onClick={() => { closeMenu(); logout(); navigate("/"); }}
                className={mobileLinkClass}
              >
                Log out
              </button>
            </>
          ) : (
            <Link to="/login" className={mobileLinkClass} onClick={closeMenu}>Log in</Link>
          )}
        </div>
      )}
    </header>
  );
}
