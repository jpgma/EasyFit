import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { ToastProvider } from "@/components/Toast";
import { router } from "@/navigation/router";
import "@/styles/index.css";

const queryClient = new QueryClient();

const root = document.getElementById("root");
if (!root) throw new Error("Elemento raiz não encontrado.");

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </QueryClientProvider>
  </StrictMode>,
);
