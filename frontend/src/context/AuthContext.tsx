import { createContext, useContext, useEffect, useState, ReactNode } from "react";
import { api } from "../api/client";
import type { MeDto, RegisterResultDto } from "../types";

interface AuthContextValue {
  user: MeDto | null;
  loading: boolean;
  register: (username: string, email: string, password: string, displayName: string) => Promise<RegisterResultDto>;
  verifyEmail: (email: string, code: string) => Promise<void>;
  resendVerification: (email: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  loginWithGoogle: (idToken: string) => Promise<void>;
  loginWithFacebook: (accessToken: string) => Promise<void>;
  forgotPassword: (email: string) => Promise<void>;
  resetPassword: (email: string, code: string, newPassword: string) => Promise<void>;
  logout: () => void;
  refreshMe: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<MeDto | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshMe = async () => {
    const token = localStorage.getItem("askme_token");
    if (!token) {
      setUser(null);
      setLoading(false);
      return;
    }
    try {
      const { data } = await api.get<MeDto>("/users/me");
      setUser(data);
    } catch {
      localStorage.removeItem("askme_token");
      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    refreshMe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const storeSession = async (token: string) => {
    localStorage.setItem("askme_token", token);
    await refreshMe();
  };

  // Registration no longer logs the user in directly - the account isn't
  // usable until the emailed code is verified via verifyEmail().
  const register = async (username: string, email: string, password: string, displayName: string) => {
    const { data } = await api.post<RegisterResultDto>("/auth/register", { username, email, password, displayName });
    return data;
  };

  const verifyEmail = async (email: string, code: string) => {
    const { data } = await api.post("/auth/verify-email", { email, code });
    await storeSession(data.token);
  };

  const resendVerification = async (email: string) => {
    await api.post("/auth/resend-verification", { email });
  };

  const login = async (email: string, password: string) => {
    const { data } = await api.post("/auth/login", { email, password });
    await storeSession(data.token);
  };

  const loginWithGoogle = async (idToken: string) => {
    const { data } = await api.post("/auth/google", { idToken });
    await storeSession(data.token);
  };

  const loginWithFacebook = async (accessToken: string) => {
    const { data } = await api.post("/auth/facebook", { accessToken });
    await storeSession(data.token);
  };

  const forgotPassword = async (email: string) => {
    await api.post("/auth/forgot-password", { email });
  };

  const resetPassword = async (email: string, code: string, newPassword: string) => {
    await api.post("/auth/reset-password", { email, code, newPassword });
  };

  const logout = () => {
    localStorage.removeItem("askme_token");
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{
      user, loading, register, verifyEmail, resendVerification, login,
      loginWithGoogle, loginWithFacebook, forgotPassword, resetPassword, logout, refreshMe,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
