import type { Evidencia } from "./types";

export function EvidenciasAtividade({
  evidencias,
}: {
  evidencias: Evidencia[];
}) {
  if (evidencias.length === 0) {
    return (
      <div className="grid aspect-[4/3] place-items-center bg-surface-muted text-sm text-muted">
        Sem comprovante anexado
      </div>
    );
  }

  return (
    <div className="flex snap-x snap-mandatory overflow-x-auto">
      {evidencias.map((evidencia) => (
        <img
          key={evidencia.id}
          src={evidencia.url}
          alt={`Comprovante ${evidencia.ordem} de ${evidencias.length}`}
          className="aspect-[4/3] w-full shrink-0 snap-center object-cover"
          loading="lazy"
        />
      ))}
    </div>
  );
}
