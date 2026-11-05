import { Flame } from "lucide-react";

const sizes = {
  sm: { tile: "size-9 rounded-xl", icon: "size-5" },
  lg: { tile: "size-16 rounded-3xl", icon: "size-8" },
} as const;

export function BrandMark({ size = "lg" }: { size?: keyof typeof sizes }) {
  return (
    <div
      aria-hidden="true"
      className={`inline-grid place-items-center bg-brand text-on-brand ${sizes[size].tile}`}
    >
      <Flame className={sizes[size].icon} strokeWidth={2.4} />
    </div>
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
