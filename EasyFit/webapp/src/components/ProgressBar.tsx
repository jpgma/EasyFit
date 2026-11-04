type ProgressBarProps = {
  label: string;
  valor: number;
  provisorio?: number;
  max: number;
};

function toPercent(valor: number, max: number): number {
  return Math.min(100, (valor / max) * 100);
}

/** Barra sólida para o valor definitivo; a parte provisória aparece em tom mais claro logo depois. */
export function ProgressBar({
  label,
  valor,
  provisorio = valor,
  max,
}: ProgressBarProps) {
  const definitivo = toPercent(valor, max);
  const pendente = toPercent(provisorio, max) - definitivo;

  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(valor, max)}
      className="flex h-2 w-full gap-0.5 overflow-hidden rounded-full bg-surface-muted"
    >
      {definitivo > 0 && (
        <div
          className="rounded-full bg-primary transition-[width] duration-700 ease-out"
          style={{ width: `${definitivo}%` }}
        />
      )}
      {pendente > 0 && (
        <div
          className="rounded-full bg-primary/35 transition-[width] duration-700 ease-out"
          style={{ width: `${pendente}%` }}
        />
      )}
    </div>
  );
}
