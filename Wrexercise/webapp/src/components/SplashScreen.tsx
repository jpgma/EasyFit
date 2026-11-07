import { BrandMark } from "@/components/BrandMark";

/** Enquanto a sessão carrega: evita mostrar o login por um instante a quem já está logado. */
export function SplashScreen() {
  return (
    <div
      role="status"
      aria-label="Carregando"
      className="flex min-h-dvh items-center justify-center"
    >
      <BrandMark />
    </div>
  );
}
