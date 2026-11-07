import { ChevronRight, Eye } from "lucide-react";
import { Link } from "react-router";
import { useFilaAvaliacao } from "@/features/atividades";

export function AvisoAvaliacao({ grupoId }: { grupoId: string }) {
  const fila = useFilaAvaliacao(grupoId);
  const primeira = fila.data?.[0];
  if (!fila.data || !primeira) return null;
  const total = fila.data.length;

  return (
    <Link
      to={`/grupos/${grupoId}/revisar/${primeira.id}`}
      className="mt-5 flex items-center gap-3 rounded-2xl border border-warning/25 bg-warning-soft p-4 tap"
    >
      <div className="grid size-10 shrink-0 place-items-center rounded-xl bg-surface text-warning">
        <Eye aria-hidden="true" className="size-5" />
      </div>
      <div className="min-w-0 flex-1 text-on-warning-soft">
        <p className="text-sm font-semibold">
          {total} {total === 1 ? "atividade espera" : "atividades esperam"} sua
          avaliação
        </p>
        <p className="text-xs">Dê uma olhada e incentive a galera</p>
      </div>
      <ChevronRight
        aria-hidden="true"
        className="size-5 shrink-0 text-on-warning-soft"
      />
    </Link>
  );
}
