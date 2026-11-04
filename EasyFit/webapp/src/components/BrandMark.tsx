import { Activity } from "lucide-react";

const sizes = {
  sm: { tile: "h-12 w-12 rounded-2xl", icon: "h-6 w-6" },
  lg: { tile: "h-16 w-16 rounded-3xl", icon: "h-8 w-8" },
} as const;

export function BrandMark({ size = "lg" }: { size?: keyof typeof sizes }) {
  return (
    <div
      aria-hidden="true"
      className={`inline-flex items-center justify-center bg-brand shadow-float ${sizes[size].tile}`}
    >
      <Activity className={`text-on-brand ${sizes[size].icon}`} />
    </div>
  );
}
