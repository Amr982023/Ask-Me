import { createContext, useContext, useEffect, useState, ReactNode } from "react";

interface ThemeContextValue {
  isDark: boolean;
  toggle: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

function getInitialTheme(): boolean {
  const stored = localStorage.getItem("askme_theme");
  if (stored) return stored === "dark";
  return window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [isDark, setIsDark] = useState(getInitialTheme);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", isDark);
    localStorage.setItem("askme_theme", isDark ? "dark" : "light");
  }, [isDark]);

  const toggle = () => setIsDark((d) => !d);

  return <ThemeContext.Provider value={{ isDark, toggle }}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
