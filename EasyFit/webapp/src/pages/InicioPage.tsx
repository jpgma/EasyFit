import { useHealth } from "@/features/health";

const statusText = {
  pending: "Verificando o servidor…",
  error: "Servidor indisponível.",
  success: "Servidor disponível.",
} as const;

export function InicioPage() {
  const { status } = useHealth();

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center gap-2 p-6">
      <h1 className="text-3xl font-semibold tracking-tight">EasyFit</h1>
      <p role="status" className="text-neutral-500 dark:text-neutral-400">
        {statusText[status]}
      </p>
    </main>
  );
}
