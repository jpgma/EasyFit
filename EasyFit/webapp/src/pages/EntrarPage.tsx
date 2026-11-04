import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, Lock, Mail } from "lucide-react";
import { useForm } from "react-hook-form";
import { Link, useLocation } from "react-router";
import { z } from "zod";
import { BrandMark } from "@/components/BrandMark";
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
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <main className="flex min-h-dvh flex-col">
      <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-6 py-12">
        <div className="mb-10 animate-slide-up text-center">
          <div className="mb-4">
            <BrandMark />
          </div>
          <h1 className="text-3xl font-bold">EasyFit</h1>
          <p className="mt-2 text-balance text-muted">
            Desafios fitness semanais com amigos. Mantenha a consistência
            juntos.
          </p>
        </div>

        <form
          noValidate
          onSubmit={(event) =>
            void handleSubmit((data) => entrar.mutate(data))(event)
          }
          className="animate-slide-up space-y-4 [animation-delay:0.1s]"
        >
          <TextField
            label="E-mail"
            icon={Mail}
            type="email"
            autoComplete="email"
            inputMode="email"
            placeholder="seu@email.com"
            error={errors.email?.message}
            {...register("email")}
          />
          <TextField
            label="Senha"
            icon={Lock}
            type="password"
            autoComplete="current-password"
            placeholder="••••••••"
            error={errors.senha?.message}
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
            <ArrowRight aria-hidden="true" className="h-5 w-5" />
          </button>

          <div className="relative pt-2">
            <div className="absolute inset-0 flex items-center pt-2">
              <div className="w-full border-t border-border" />
            </div>
            <div className="relative flex justify-center">
              <span className="bg-background px-3 text-sm text-muted">ou</span>
            </div>
          </div>

          <Link
            to="/criar-conta"
            state={
              state?.conviteToken
                ? { conviteToken: state.conviteToken }
                : undefined
            }
            className="btn-secondary w-full"
          >
            Criar conta
          </Link>
        </form>

        <p className="mt-8 text-center text-2xs text-muted">
          EasyFit é um app privado para grupos de amigos.
        </p>
      </div>
    </main>
  );
}
