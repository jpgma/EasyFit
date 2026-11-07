import { useLocation, useNavigate } from "react-router";

type EstadoGaleria = { galeria?: { chave: string; indice: number } };

/**
 * A galeria aberta fica numa entrada própria do histórico: o botão Voltar do
 * celular fecha a galeria em vez de sair da tela.
 */
export function useGaleria(chave: string) {
  const location = useLocation();
  const navigate = useNavigate();
  const state = location.state as EstadoGaleria | null;
  const galeria = state?.galeria?.chave === chave ? state.galeria : undefined;

  return {
    aberta: galeria !== undefined,
    indiceInicial: galeria?.indice ?? 0,
    abrir(indice: number) {
      const { pathname, search, hash } = location;
      void navigate(
        { pathname, search, hash },
        {
          state: { ...state, galeria: { chave, indice } },
          preventScrollReset: true,
        },
      );
    },
    fechar() {
      void navigate(-1);
    },
  };
}
