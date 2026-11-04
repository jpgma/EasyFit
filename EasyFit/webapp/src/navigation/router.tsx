import { createBrowserRouter } from "react-router";

// Toda página é carregada sob demanda para manter o bundle inicial pequeno.
export const router = createBrowserRouter([
  {
    path: "/",
    lazy: async () => {
      const { InicioPage } = await import("@/pages/InicioPage");
      return { Component: InicioPage };
    },
  },
]);
