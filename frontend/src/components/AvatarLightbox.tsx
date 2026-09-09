interface Props {
  src: string;
  name: string;
  onClose: () => void;
}

// Full-size view of a profile photo - triggered by clicking the avatar.
export default function AvatarLightbox({ src, name, onClose }: Props) {
  return (
    <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50" onClick={onClose}>
      <img src={src} alt={name} className="max-w-full max-h-full rounded-2xl object-contain" onClick={(e) => e.stopPropagation()} />
    </div>
  );
}
