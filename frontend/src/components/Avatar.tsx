import { useState } from "react";

interface Props {
  src?: string | null;
  name: string;
  className?: string;
}

// Falls back to the person's initial whenever there's no image URL, OR the
// image URL fails to actually load (404, network error, misconfigured
// storage). Without the onError handler, a broken <img> shows the browser's
// broken-image icon plus its alt text rendered inline - which is exactly
// what you were seeing.
export default function Avatar({ src, name, className = "" }: Props) {
  const [failed, setFailed] = useState(false);
  const showImage = !!src && !failed;

  return (
    <div className={`bg-brand-100 dark:bg-brand-900 text-brand-700 dark:text-brand-400 flex items-center justify-center font-bold overflow-hidden shrink-0 ${className}`}>
      {showImage ? (
        <img src={src} alt={name} className="w-full h-full object-cover" onError={() => setFailed(true)} />
      ) : (
        <span>{name.charAt(0).toUpperCase()}</span>
      )}
    </div>
  );
}
