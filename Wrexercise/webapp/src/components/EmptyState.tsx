import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

type EmptyStateProps = {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: ReactNode;
};

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center px-6 py-10 text-center">
      <div className="relative mb-4 grid size-16 place-items-center">
        <div aria-hidden="true" className="absolute -inset-3 halftone" />
        <div className="relative grid size-14 rotate-[-4deg] place-items-center rounded-xl border-2 border-outline bg-soft text-on-soft shadow-sticker">
          <Icon aria-hidden="true" className="size-7" />
        </div>
      </div>
      <h2 className="text-xl uppercase">{title}</h2>
      <p className="mt-1 max-w-xs text-sm text-muted">{description}</p>
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
