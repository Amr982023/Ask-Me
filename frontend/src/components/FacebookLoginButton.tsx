declare global {
  interface Window {
    FB?: {
      init: (config: Record<string, unknown>) => void;
      login: (callback: (response: FacebookLoginResponse) => void, options: Record<string, unknown>) => void;
    };
    fbAsyncInit?: () => void;
  }
}

interface FacebookLoginResponse {
  authResponse?: { accessToken: string };
}

let fbSdkLoading: Promise<void> | null = null;

function loadFacebookSdk(appId: string): Promise<void> {
  if (window.FB) return Promise.resolve();
  if (fbSdkLoading) return fbSdkLoading;

  fbSdkLoading = new Promise((resolve) => {
    window.fbAsyncInit = () => {
      window.FB?.init({ appId, cookie: true, xfbml: false, version: "v19.0" });
      resolve();
    };
    const script = document.createElement("script");
    script.src = "https://connect.facebook.net/en_US/sdk.js";
    script.async = true;
    document.body.appendChild(script);
  });
  return fbSdkLoading;
}

export default function FacebookLoginButton({ onToken }: { onToken: (accessToken: string) => void }) {
  const appId = import.meta.env.VITE_FACEBOOK_APP_ID as string | undefined;
  if (!appId) return null; // not configured - hide rather than show a broken button

  const login = async () => {
    await loadFacebookSdk(appId);
    window.FB?.login(
      (response) => {
        if (response.authResponse?.accessToken) onToken(response.authResponse.accessToken);
      },
      { scope: "email" }
    );
  };

  return (
    <button type="button" onClick={login} className="btn-secondary w-full text-sm">
      Continue with Facebook
    </button>
  );
}
