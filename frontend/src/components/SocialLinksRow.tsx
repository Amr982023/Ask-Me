import type { SocialLinks } from "../types";

const ICON_LABELS: { key: keyof SocialLinks; label: string }[] = [
  { key: "websiteUrl", label: "🌐" },
  { key: "twitterUrl", label: "𝕏" },
  { key: "facebookUrl", label: "f" },
  { key: "githubUrl", label: "GH" },
  { key: "linkedinUrl", label: "in" },
  { key: "youtubeUrl", label: "▶" },
  { key: "tiktokUrl", label: "♪" },
];

export default function SocialLinksRow({ links }: { links: SocialLinks }) {
  const present = ICON_LABELS.filter(({ key }) => links[key]);
  if (present.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {present.map(({ key, label }) => (
        <a
          key={key}
          href={links[key]!}
          target="_blank"
          rel="noopener noreferrer"
          className="w-9 h-9 rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-brand-100 dark:hover:bg-brand-950 hover:text-brand-700 dark:hover:text-brand-400 text-slate-600 dark:text-slate-400 flex items-center justify-center text-sm font-semibold transition-colors"
        >
          {label}
        </a>
      ))}
    </div>
  );
}
