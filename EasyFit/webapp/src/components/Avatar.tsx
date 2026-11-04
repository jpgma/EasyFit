type AvatarProps = {
  nome: string;
};

function getInitials(nome: string): string {
  const palavras = nome.trim().split(/\s+/);
  const primeira = palavras[0]?.charAt(0) ?? "";
  const ultima = palavras.length > 1 ? (palavras.at(-1)?.charAt(0) ?? "") : "";
  return (primeira + ultima).toUpperCase();
}

export function Avatar({ nome }: AvatarProps) {
  return (
    <span
      aria-hidden="true"
      className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-soft text-sm font-bold text-on-soft"
    >
      {getInitials(nome)}
    </span>
  );
}
