import {
  Activity,
  BicepsFlexed,
  Bike,
  Dumbbell,
  Flower2,
  Footprints,
  PersonStanding,
  Trophy,
  Waves,
  type LucideIcon,
} from "lucide-react";
import type { TipoAtividade } from "./types";

export const tipoAtividadeIcones: Record<TipoAtividade, LucideIcon> = {
  Musculacao: Dumbbell,
  Corrida: PersonStanding,
  Caminhada: Footprints,
  Ciclismo: Bike,
  Natacao: Waves,
  Esporte: Trophy,
  Yoga: Flower2,
  Calistenia: BicepsFlexed,
  Outro: Activity,
};

export function TipoAtividadeIcone({
  tipo,
  className = "size-10",
}: {
  tipo: TipoAtividade;
  className?: string;
}) {
  const Icon = tipoAtividadeIcones[tipo];
  return (
    <div
      aria-hidden="true"
      className={`grid shrink-0 place-items-center rounded-xl bg-soft text-on-soft ${className}`}
    >
      <Icon className="size-5" />
    </div>
  );
}
