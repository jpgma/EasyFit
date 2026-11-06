const sizes = { sm: "size-9", lg: "size-16" } as const;

/** O mesmo ícone do aplicativo, gerado por scripts/gerar-icones.sh. */
export function BrandMark({ size = "lg" }: { size?: keyof typeof sizes }) {
  return (
    <img
      src="/icon-192.png"
      alt=""
      aria-hidden="true"
      draggable={false}
      className={`inline-block ${sizes[size]}`}
    />
  );
}

export function Logo({ className = "" }: { className?: string }) {
  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <BrandMark size="sm" />
      <span className="font-display text-xl font-bold tracking-tight">
        EasyFit
      </span>
    </div>
  );
}
