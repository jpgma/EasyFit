import { createBrowserRouter, Navigate } from "react-router";
import { SplashScreen } from "@/components/SplashScreen";
import { AppLayout } from "@/navigation/AppLayout";
import { RequireAnonimo, RequireSessao } from "@/navigation/guards";

// Toda página é carregada sob demanda para manter o bundle inicial pequeno.
export const router = createBrowserRouter([
  {
    HydrateFallback: SplashScreen,
    children: [
      {
        Component: RequireAnonimo,
        children: [
          {
            path: "/entrar",
            lazy: async () => {
              const { EntrarPage } = await import("@/pages/EntrarPage");
              return { Component: EntrarPage };
            },
          },
          {
            path: "/criar-conta",
            lazy: async () => {
              const { CriarContaPage } = await import("@/pages/CriarContaPage");
              return { Component: CriarContaPage };
            },
          },
        ],
      },
      {
        Component: RequireSessao,
        children: [
          {
            Component: AppLayout,
            children: [
              {
                path: "/",
                lazy: async () => {
                  const { RaizPage } = await import("@/pages/RaizPage");
                  return { Component: RaizPage };
                },
              },
              {
                path: "/perfil",
                lazy: async () => {
                  const { PerfilPage } = await import("@/pages/PerfilPage");
                  return { Component: PerfilPage };
                },
              },
              {
                path: "/grupos/novo",
                lazy: async () => {
                  const { CriarGrupoPage } =
                    await import("@/pages/CriarGrupoPage");
                  return { Component: CriarGrupoPage };
                },
              },
              {
                path: "/grupos/:id",
                lazy: async () => {
                  const { GrupoPage } = await import("@/pages/GrupoPage");
                  return { Component: GrupoPage };
                },
              },
              {
                path: "/grupos/:id/inicio",
                lazy: async () => {
                  const { InicioPage } = await import("@/pages/InicioPage");
                  return { Component: InicioPage };
                },
              },
              {
                path: "/grupos/:id/atividades",
                lazy: async () => {
                  const { AtividadesPage } =
                    await import("@/pages/AtividadesPage");
                  return { Component: AtividadesPage };
                },
              },
              {
                path: "/grupos/:id/atividades/nova",
                lazy: async () => {
                  const { RegistrarAtividadePage } =
                    await import("@/pages/RegistrarAtividadePage");
                  return { Component: RegistrarAtividadePage };
                },
              },
              {
                path: "/grupos/:id/revisar/:atividadeId",
                lazy: async () => {
                  const { RevisarPage } = await import("@/pages/RevisarPage");
                  return { Component: RevisarPage };
                },
              },
              {
                path: "/grupos/:id/convidar",
                lazy: async () => {
                  const { ConvidarPage } = await import("@/pages/ConvidarPage");
                  return { Component: ConvidarPage };
                },
              },
              {
                path: "/grupos/:id/desafios/novo",
                lazy: async () => {
                  const { CriarDesafioPage } =
                    await import("@/pages/CriarDesafioPage");
                  return { Component: CriarDesafioPage };
                },
              },
              {
                path: "/desafios/:id",
                lazy: async () => {
                  const { DesafioPage } = await import("@/pages/DesafioPage");
                  return { Component: DesafioPage };
                },
              },
              {
                path: "/atividades/:id",
                lazy: async () => {
                  const { AtividadePage } =
                    await import("@/pages/AtividadePage");
                  return { Component: AtividadePage };
                },
              },
            ],
          },
        ],
      },
      {
        // Fora dos dois guards: serve tanto a quem tem conta quanto a quem ainda vai criar.
        path: "/convite",
        lazy: async () => {
          const { ConvitePage } = await import("@/pages/ConvitePage");
          return { Component: ConvitePage };
        },
      },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
