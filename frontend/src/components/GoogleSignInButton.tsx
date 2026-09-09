import { useEffect, useRef } from "react";
import { useTheme } from "../context/ThemeContext";

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: { client_id: string; callback: (resp: { credential: string }) => void }) => void;
          renderButton: (el: HTMLElement, options: Record<string, unknown>) => void;
        };
      };
    };
  }
}

// Renders Google's own "Sign in with Google" button once the Identity
// Services script loads, and forwards the ID token it produces up to the
// caller - the backend (GoogleTokenValidator) verifies it independently, so
// this component never has to be trusted on its own.
export default function GoogleSignInButton({ onToken }: { onToken: (idToken: string) => void }) {
  const divRef = useRef<HTMLDivElement>(null);
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  const { isDark } = useTheme();

  useEffect(() => {
    if (!clientId) return;

    const render = () => {
      window.google?.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => onToken(response.credential),
      });
      if (divRef.current) {
        divRef.current.innerHTML = ""; // clear before re-render on theme change
        // Google's own themed button - "filled_black" reads correctly on a
        // dark page, "outline" on a light one, instead of using one theme
        // that clashes with whichever mode is active.
        window.google?.accounts.id.renderButton(divRef.current, {
          theme: isDark ? "filled_black" : "outline",
          size: "large",
          width: 280,
        });
      }
    };

    if (window.google) {
      render();
      return;
    }

    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.onload = render;
    document.body.appendChild(script);
    // Intentionally not removing the script on unmount - Google's client is
    // safe to keep loaded across page navigations within the SPA.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [clientId, isDark]);

  if (!clientId) return null; // not configured - hide rather than show a broken button
  return <div ref={divRef} className="flex justify-center" />;
}
