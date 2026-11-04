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
        ],
      },
      { path: "*", element: <Navigate to="/" replace /> },
    ],
  },
]);
