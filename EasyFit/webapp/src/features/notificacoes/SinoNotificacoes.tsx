import { Bell } from "lucide-react";
import { Link } from "react-router";
import { useTotalNaoLidas } from "./useNotificacoes";

export function SinoNotificacoes() {
  const { data: total = 0 } = useTotalNaoLidas();

  return (
    <Link
      to="/notificacoes"
      aria-label={
        total > 0 ? `Notificações: ${total} não lidas` : "Notificações"
      }
      className="relative grid size-10 shrink-0 place-items-center rounded-full bg-surface shadow-card tap"
    >
      <Bell aria-hidden="true" className="size-5" />
      {total > 0 && (
        <span
          aria-hidden="true"
          className="absolute top-2 right-2 size-2 rounded-full bg-danger"
        />
      )}
    </Link>
  );
}
