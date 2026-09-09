import { useEffect, useRef, useState } from "react";

/**
 * A one-shot countdown in seconds, for "resend code" cooldowns. Call start()
 * right after a code is sent; secondsLeft ticks down to 0, at which point
 * the resend button re-enables itself.
 */
export function useCountdown(initialSeconds: number) {
  const [secondsLeft, setSecondsLeft] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const start = (seconds: number = initialSeconds) => {
    setSecondsLeft(seconds);
    if (intervalRef.current) clearInterval(intervalRef.current);
    intervalRef.current = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          if (intervalRef.current) clearInterval(intervalRef.current);
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  };

  useEffect(() => () => { if (intervalRef.current) clearInterval(intervalRef.current); }, []);

  return { secondsLeft, start, isActive: secondsLeft > 0 };
}
