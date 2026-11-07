import { ChevronLeft } from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router";

type PageHeaderProps = {
  title: string;
  back?: string;
  subtitle?: string;
  action?: ReactNode;
};

export function PageHeader({ title, back, subtitle, action }: PageHeaderProps) {
  return (
    <header className="mb-5 flex items-center gap-3">
      {back && (
        <Link
          to={back}
          aria-label="Voltar"
          className="grid size-10 shrink-0 place-items-center rounded-full bg-surface shadow-card tap"
        >
          <ChevronLeft aria-hidden="true" className="size-5" />
        </Link>
      )}
      <div className="min-w-0 flex-1">
        <h1 className="truncate text-2xl font-bold">{title}</h1>
        {subtitle && <p className="text-sm text-muted">{subtitle}</p>}
      </div>
      {action}
    </header>
  );
}

export function SectionTitle({
  children,
  action,
}: {
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="mt-7 mb-3 flex items-center justify-between gap-3">
      <h2 className="text-lg font-bold">{children}</h2>
      {action}
    </div>
  );
}
