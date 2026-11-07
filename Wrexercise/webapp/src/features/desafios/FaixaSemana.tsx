import { addDias, getHojeNoFuso } from "./datas";

const letras = ["S", "T", "Q", "Q", "S", "S", "D"];

/** As semanas de um desafio sempre começam na segunda-feira. */
export function FaixaSemana({
  inicio,
  diasComTreino,
  fusoHorario,
}: {
  inicio: string;
  diasComTreino: string[];
  fusoHorario: string;
}) {
  const hoje = getHojeNoFuso(fusoHorario);

  return (
    <ul
      aria-label="Dias da semana"
      className="mt-4 grid grid-cols-7 gap-1.5 text-center text-2xs text-muted"
    >
      {letras.map((letra, indice) => {
        const dia = addDias(inicio, indice);
        const treinou = diasComTreino.includes(dia);
        const eHoje = dia === hoje;
        return (
          <li
            key={dia}
            aria-current={eHoje ? "date" : undefined}
            aria-label={`Dia ${Number(dia.slice(8))}${treinou ? ": treinou" : ""}`}
          >
            <div
              className={`mx-auto mb-1 grid size-8 place-items-center rounded-full text-xs font-semibold ${
                treinou
                  ? "bg-primary text-on-primary"
                  : eHoje
                    ? "border-2 border-primary text-primary"
                    : "bg-surface-muted"
              }`}
            >
              {Number(dia.slice(8))}
            </div>
            {letra}
          </li>
        );
      })}
    </ul>
  );
}
