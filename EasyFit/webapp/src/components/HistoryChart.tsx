type Coluna = {
  chave: string | number;
  rotulo: string;
  valor: number;
  provisorio: number;
};

type HistoryChartProps = {
  titulo: string;
  meta: number;
  colunas: Coluna[];
};

const alturaEmPx = 112;

/** Barras simples em CSS: uma biblioteca de gráficos pesaria mais que o app inteiro. */
export function HistoryChart({ titulo, meta, colunas }: HistoryChartProps) {
  const topo =
    Math.max(meta, ...colunas.map(({ provisorio }) => provisorio)) + 1;

  return (
    <div className="card p-4">
      <p className="text-sm font-semibold">{titulo}</p>
      <p className="text-xs text-muted">Sessões por semana · linha = meta</p>
      <div
        className="relative mt-4 flex items-end gap-2"
        style={{ height: alturaEmPx }}
      >
        <div
          aria-hidden="true"
          className="absolute inset-x-0 border-t border-dashed border-primary/50"
          style={{ bottom: `${(meta / topo) * 100}%` }}
        />
        {colunas.map(({ chave, rotulo, valor, provisorio }) => (
          <div
            key={chave}
            role="img"
            aria-label={`${rotulo}: ${valor} verificadas de ${provisorio} registradas`}
            className="flex flex-1 flex-col justify-end gap-px"
          >
            {provisorio > valor && (
              <div
                className="w-full rounded-t-md bg-primary/35"
                style={{
                  height: `${((provisorio - valor) / topo) * alturaEmPx}px`,
                }}
              />
            )}
            {valor > 0 && (
              <div
                className={`w-full rounded-md ${valor >= meta ? "bg-primary" : "bg-muted/40"}`}
                style={{ height: `${(valor / topo) * alturaEmPx}px` }}
              />
            )}
          </div>
        ))}
      </div>
      <div className="mt-1 flex gap-2">
        {colunas.map(({ chave, rotulo }) => (
          <span
            key={chave}
            className="min-w-0 flex-1 truncate text-center text-[10px] text-muted"
          >
            {rotulo}
          </span>
        ))}
      </div>
    </div>
  );
}
