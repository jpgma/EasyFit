import { createBrowserRouter, Navigate } from "react-router";
import { SplashScreen } from "@/components/SplashScreen";
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
            path: "/",
            lazy: async () => {
              const { InicioPage } = await import("@/pages/InicioPage");
              return { Component: InicioPage };
            },
          },
          {
            path: "/grupos/novo",
            lazy: async () => {
              const { CriarGrupoPage } = await import("@/pages/CriarGrupoPage");
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
