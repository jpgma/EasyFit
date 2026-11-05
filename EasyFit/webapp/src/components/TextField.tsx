import type { LucideIcon } from "lucide-react";
import { useId, type ComponentProps, type ReactNode } from "react";

type TextFieldProps = ComponentProps<"input"> & {
  label: string;
  icon?: LucideIcon;
  error?: string;
  hint?: string;
  trailing?: ReactNode;
};

export function TextField({
  label,
  icon: Icon,
  error,
  hint,
  trailing,
  ...input
}: TextFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;

  return (
    <div>
      <label htmlFor={id} className="input-label">
        {label}
      </label>
      <div className="relative">
        {Icon && (
          <Icon
            aria-hidden="true"
            className="absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted"
          />
        )}
        <input
          {...input}
          id={id}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : hint ? hintId : undefined}
          className={`input-field ${Icon ? "pl-10" : ""} ${trailing ? "pr-11" : ""} ${error ? "border-danger" : ""}`}
        />
        {trailing && (
          <div className="absolute inset-y-0 right-0 flex items-center pr-3">
            {trailing}
          </div>
        )}
      </div>
      {error ? (
        <p id={errorId} className="input-error">
          {error}
        </p>
      ) : (
        hint && (
          <p id={hintId} className="mt-1.5 text-xs text-muted">
            {hint}
          </p>
        )
      )}
    </div>
  );
}
