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

/** Arco sólido para o definitivo; o provisório aparece em tom mais claro logo depois. Lime ao completar. */
export function ProgressRing({
  label,
  valor,
  provisorio = valor,
  max,
  size = 132,
  stroke = 12,
  children,
}: ProgressRingProps) {
  // Reserva 2px de cada lado para o contorno.
  const raio = (size - stroke - 4) / 2;
  const circunferencia = 2 * Math.PI * raio;
  const fracao = (n: number) => (max > 0 ? Math.min(n / max, 1) : 0);
  const definitivo = fracao(valor);
  const comProvisorio = fracao(provisorio);
  const circulo = { cx: size / 2, cy: size / 2, r: raio, fill: "none" };
  const completo = definitivo >= 1;

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
      {completo && (
        // Explosão de quadrinho atrás do anel, uma vez só ao completar.
        <div
          aria-hidden="true"
          className="absolute -inset-2 animate-burst bg-reward burst"
        />
      )}
      <svg
        width={size}
        height={size}
        className={`relative -rotate-90 ${completo ? "animate-pop" : ""}`}
        aria-hidden="true"
      >
        {/* Contornos do anel: dão o traço de adesivo e separam o coral do fundo claro. */}
        <circle
          cx={size / 2}
          cy={size / 2}
          r={raio}
          fill="none"
          stroke="var(--color-outline)"
          strokeWidth={stroke + 4}
        />
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
            strokeLinecap="butt"
            strokeDasharray={circunferencia}
            strokeDashoffset={circunferencia * (1 - comProvisorio)}
          />
        )}
        {definitivo > 0 && (
          <circle
            {...circulo}
            stroke={completo ? "var(--color-progress)" : "var(--color-primary)"}
            strokeWidth={stroke}
            strokeLinecap="butt"
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
