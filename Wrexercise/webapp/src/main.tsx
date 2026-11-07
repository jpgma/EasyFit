import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { AvisosDoApp } from "@/components/AvisosDoApp";
import { ToastProvider } from "@/components/Toast";
import { router } from "@/navigation/router";
import { registrarInstalacao } from "@/services/instalacao";
import { registrarServiceWorker } from "@/services/serviceWorker";
import "@/styles/index.css";

const queryClient = new QueryClient({
  defaultOptions: {
    // Sem fila offline: uma escrita sem rede falha na hora, em vez de esperar pausada em silêncio.
    mutations: { networkMode: "always" },
  },
});

registrarServiceWorker();
registrarInstalacao();

const root = document.getElementById("root");
if (!root) throw new Error("Elemento raiz não encontrado.");

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AvisosDoApp />
        <RouterProvider router={router} />
      </ToastProvider>
    </QueryClientProvider>
  </StrictMode>,
);
