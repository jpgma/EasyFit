import type { LucideIcon } from "lucide-react";

type EmptyStateProps = {
  icon: LucideIcon;
  title: string;
  description: string;
};

export function EmptyState({
  icon: Icon,
  title,
  description,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-surface-muted text-muted">
        <Icon aria-hidden="true" className="h-8 w-8" />
      </div>
      <h2 className="mb-1 text-base font-bold text-foreground">{title}</h2>
      <p className="max-w-xs text-sm text-muted">{description}</p>
    </div>
  );
}
