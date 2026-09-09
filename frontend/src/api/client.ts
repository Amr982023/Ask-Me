import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "https://localhost:57345/api",
});

// Attach the JWT (if present) to every request. Token lives only in memory +
// localStorage on the client - never sent anywhere except our own API.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("askme_token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem("askme_token");
    }
    return Promise.reject(err);
  }
);

export function getErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    return err.response?.data?.error || err.message || "Something went wrong.";
  }
  return "Something went wrong.";
}
