import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";

type ToastContextValue = { showToast: (mensagem: string) => void };

const ToastContext = createContext<ToastContextValue | null>(null);

const duracaoEmMs = 3000;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [mensagem, setMensagem] = useState<string | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);

  const showToast = useCallback((nova: string) => {
    clearTimeout(timer.current);
    setMensagem(nova);
    timer.current = setTimeout(() => setMensagem(null), duracaoEmMs);
  }, []);

  const value = useMemo(() => ({ showToast }), [showToast]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        role="status"
        aria-live="polite"
        className="pointer-events-none fixed inset-x-0 top-4 z-50 flex justify-center px-4"
      >
        {mensagem && (
          <p className="animate-slide-up rounded-lg border-2 border-outline bg-surface px-4 py-3 text-sm font-semibold shadow-sticker">
            {mensagem}
          </p>
        )}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const value = useContext(ToastContext);
  if (!value) throw new Error("ToastProvider ausente.");
  return value;
}
