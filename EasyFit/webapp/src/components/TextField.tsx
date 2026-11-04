import type { LucideIcon } from "lucide-react";
import { useId, type ComponentProps } from "react";

type TextFieldProps = ComponentProps<"input"> & {
  label: string;
  icon: LucideIcon;
  error?: string;
};

export function TextField({
  label,
  icon: Icon,
  error,
  ...input
}: TextFieldProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div>
      <label htmlFor={id} className="input-label">
        {label}
      </label>
      <div className="relative">
        <Icon
          aria-hidden="true"
          className="absolute top-1/2 left-3.5 h-5 w-5 -translate-y-1/2 text-placeholder"
        />
        <input
          {...input}
          id={id}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={`input-field pl-11 ${error ? "border-danger" : ""}`}
        />
      </div>
      {error && (
        <p id={errorId} className="input-error">
          {error}
        </p>
      )}
    </div>
  );
}
