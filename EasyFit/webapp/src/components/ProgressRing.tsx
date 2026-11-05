import type { ReactNode } from "react";

type ProgressRingProps = {
  label: string;
  valor: number;
  /** Definitivas somadas às que ainda aguardam verificação. */
  provisorio?: number;
  max: number;
  size?: number;
  stroke?: number;
  children?: ReactNode;
};

/** Arco sólido para o definitivo; o provisório aparece em tom mais claro logo depois. */
export function ProgressRing({
  label,
  valor,
  provisorio = valor,
  max,
  size = 132,
  stroke = 12,
  children,
}: ProgressRingProps) {
  const raio = (size - stroke) / 2;
  const circunferencia = 2 * Math.PI * raio;
  const fracao = (n: number) => (max > 0 ? Math.min(n / max, 1) : 0);
  const definitivo = fracao(valor);
  const comProvisorio = fracao(provisorio);
  const circulo = { cx: size / 2, cy: size / 2, r: raio, fill: "none" };

  return (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={Math.min(valor, max)}
      className="relative shrink-0"
      style={{ width: size, height: size }}
    >
      <svg width={size} height={size} className="-rotate-90" aria-hidden="true">
        <circle
          {...circulo}
          stroke="var(--color-surface-muted)"
          strokeWidth={stroke}
        />
        {comProvisorio > definitivo && (
          <circle
            {...circulo}
            stroke="var(--color-primary)"
            strokeOpacity={0.35}
            strokeWidth={stroke}
            strokeLinecap="round"
            strokeDasharray={circunferencia}
            strokeDashoffset={circunferencia * (1 - comProvisorio)}
          />
        )}
        {definitivo > 0 && (
          <circle
            {...circulo}
            stroke={
              definitivo >= 1 ? "var(--color-success)" : "var(--color-primary)"
            }
            strokeWidth={stroke}
            strokeLinecap="round"
            strokeDasharray={circunferencia}
            strokeDashoffset={circunferencia * (1 - definitivo)}
            style={{ transition: "stroke-dashoffset .8s ease" }}
          />
        )}
      </svg>
      <div className="absolute inset-0 grid place-items-center text-center">
        {children}
      </div>
    </div>
  );
}
