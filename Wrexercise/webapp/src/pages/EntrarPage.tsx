import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, EyeOff, Lock, Mail } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useLocation } from "react-router";
import { z } from "zod";
import { Logo } from "@/components/BrandMark";
import { TextField } from "@/components/TextField";
import { useEntrar } from "@/features/autenticacao";
import { getErrorMessage } from "@/services/api";

// O esquema fica na página para o Zod só entrar no bundle desta tela lazy.
const schema = z.object({
  email: z.string().trim().min(1, "Digite seu e-mail"),
  senha: z.string().min(1, "Digite sua senha"),
});

type FormData = z.infer<typeof schema>;

export function EntrarPage() {
  const entrar = useEntrar();
  const { state } = useLocation() as {
    state: { conviteToken?: string } | null;
  };
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden border-r-2 border-outline bg-brand p-12 text-on-brand lg:flex">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-y-0 right-0 w-1/2 halftone-ink"
        />
        <p className="relative font-display text-xl uppercase">Wrexercise</p>
        <p className="relative font-display text-6xl leading-none uppercase">
          Treino bom é treino com a turma.
        </p>
        <p className="relative text-sm font-semibold">
          Grupos privados de até 10 amigos.
        </p>
      </div>

      <main className="flex flex-col justify-center px-6 py-10">
        <div className="mx-auto w-full max-w-sm animate-slide-up">
          <Logo />
          <h1 className="mt-10 text-3xl uppercase">Bora treinar?</h1>
          <p className="mt-2 text-muted">
            Metas semanais com seus amigos, comprovantes e muito incentivo.
          </p>

          <form
            noValidate
            onSubmit={(event) =>
              void handleSubmit((data) => entrar.mutate(data))(event)
            }
            className="mt-8 space-y-4"
          >
            <TextField
              label="E-mail"
              icon={Mail}
              type="email"
              autoComplete="email"
              inputMode="email"
              placeholder="voce@email.com"
              error={errors.email?.message}
              {...register("email")}
            />
            <TextField
              label="Senha"
              icon={Lock}
              type={mostrarSenha ? "text" : "password"}
              autoComplete="current-password"
              error={errors.senha?.message}
              trailing={
                <button
                  type="button"
                  aria-label={mostrarSenha ? "Ocultar senha" : "Mostrar senha"}
                  className="text-muted"
                  onClick={() => setMostrarSenha(!mostrarSenha)}
                >
                  {mostrarSenha ? (
                    <EyeOff aria-hidden="true" className="size-4" />
                  ) : (
                    <Eye aria-hidden="true" className="size-4" />
                  )}
                </button>
              }
              {...register("senha")}
            />

            {entrar.isError && (
              <p role="alert" className="form-error">
                {getErrorMessage(entrar.error)}
              </p>
            )}

            <button
              type="submit"
              className="btn-primary w-full"
              disabled={entrar.isPending}
            >
              {entrar.isPending ? "Entrando…" : "Entrar"}
            </button>
          </form>

          <div className="mt-8 rounded-2xl border border-dashed border-border-strong p-4 text-center text-sm">
            <p className="text-muted">Recebeu um convite de um amigo?</p>
            <Link
              to="/criar-conta"
              state={
                state?.conviteToken
                  ? { conviteToken: state.conviteToken }
                  : undefined
              }
              className="mt-1 inline-block font-semibold text-link"
            >
              Criar conta com convite
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}
