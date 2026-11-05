type SegmentsProps = {
  label: string;
  valor: number;
  provisorio?: number;
  max: number;
};

function getCor(
  indice: number,
  valor: number,
  provisorio: number,
  max: number,
): string {
  if (indice < valor) return valor >= max ? "bg-success" : "bg-primary";
  return indice < provisorio ? "bg-primary/35" : "bg-surface-muted";
}

/** Uma barra por sessão da meta: sólida se definitiva, clara se ainda provisória. */
export function Segments({
  label,
  valor,
  provisorio = valor,
  max,
}: SegmentsProps) {
  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(valor, max)}
      className="flex gap-1"
    >
      {Array.from({ length: max }, (_, indice) => (
        <div
          key={indice}
          className={`h-1.5 flex-1 rounded-full ${getCor(indice, valor, provisorio, max)}`}
        />
      ))}
    </div>
  );
}
