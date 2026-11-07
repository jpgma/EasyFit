import { useEffect, useRef } from "react";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  pending?: boolean;
  error?: string;
  onConfirm: () => void;
  onCancel: () => void;
};

/** Usa o `<dialog>` nativo, que já cuida do foco, da tecla Esc e do fundo. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  pending = false,
  error,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="confirm-dialog-title"
      onCancel={(event) => {
        event.preventDefault();
        if (!pending) onCancel();
      }}
      className="card m-auto w-[calc(100%-2rem)] max-w-sm rounded-3xl p-6 text-foreground backdrop:bg-scrim backdrop:backdrop-blur-sm"
    >
      <h2 id="confirm-dialog-title" className="mb-2 text-lg font-bold">
        {title}
      </h2>
      <p className="mb-6 text-sm leading-relaxed text-muted">{message}</p>
      {error && (
        <p role="alert" className="form-error mb-4">
          {error}
        </p>
      )}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          className="btn-ghost"
          disabled={pending}
          onClick={onCancel}
        >
          Cancelar
        </button>
        <button
          type="button"
          className="btn-danger"
          disabled={pending}
          onClick={onConfirm}
        >
          {confirmLabel}
        </button>
      </div>
    </dialog>
  );
}
