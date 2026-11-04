import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, ArrowRight, Lock, Mail, User } from "lucide-react";
import { useForm } from "react-hook-form";
import { Link } from "react-router";
import { z } from "zod";
import { BrandMark } from "@/components/BrandMark";
import { TextField } from "@/components/TextField";
import { useRegistrar } from "@/features/autenticacao";
import { getErrorMessage } from "@/services/api";

// Os limites acompanham Features/Autenticacao/Rules.cs; o backend é quem decide.
// O esquema fica na página para o Zod só entrar no bundle desta tela lazy.
const schema = z
  .object({
    nomeExibicao: z
      .string()
      .trim()
      .min(1, "Digite seu nome")
      .max(80, "Use no máximo 80 caracteres"),
    email: z
      .string()
      .trim()
      .max(254, "E-mail inválido")
      .regex(/^[^\s@]+@[^\s@]+$/, "E-mail inválido"),
    senha: z
      .string()
      .min(8, "Mínimo de 8 caracteres")
      .max(128, "Use no máximo 128 caracteres"),
    confirmarSenha: z.string(),
  })
  .refine((data) => data.senha === data.confirmarSenha, {
    message: "As senhas não coincidem",
    path: ["confirmarSenha"],
  });

type FormData = z.infer<typeof schema>;

export function CriarContaPage() {
  const registrar = useRegistrar();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <main className="flex min-h-dvh flex-col">
      <div className="mx-auto w-full max-w-md px-6 pt-6">
        <Link to="/entrar" className="btn-ghost -ml-2">
          <ArrowLeft aria-hidden="true" className="h-5 w-5" />
          Voltar
        </Link>
      </div>

      <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-6 py-8">
        <div className="mb-8 animate-slide-up">
          <div className="mb-3">
            <BrandMark size="sm" />
          </div>
          <h1 className="text-2xl font-bold">Criar conta</h1>
          <p className="mt-1.5 text-sm text-balance text-muted">
            Crie sua conta para participar dos desafios do seu grupo.
          </p>
        </div>

        <form
          noValidate
          onSubmit={(event) =>
            void handleSubmit(({ nomeExibicao, email, senha }) =>
              registrar.mutate({ nomeExibicao, email, senha }),
            )(event)
          }
          className="animate-slide-up space-y-4 [animation-delay:0.1s]"
        >
          <TextField
            label="Nome"
            icon={User}
            type="text"
            autoComplete="name"
            placeholder="Seu nome"
            error={errors.nomeExibicao?.message}
            {...register("nomeExibicao")}
          />
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
            autoComplete="new-password"
            placeholder="Mínimo de 8 caracteres"
            error={errors.senha?.message}
            {...register("senha")}
          />
          <TextField
            label="Confirmar senha"
            icon={Lock}
            type="password"
            autoComplete="new-password"
            placeholder="Repita a senha"
            error={errors.confirmarSenha?.message}
            {...register("confirmarSenha")}
          />

          {registrar.isError && (
            <p role="alert" className="form-error">
              {getErrorMessage(registrar.error)}
            </p>
          )}

          <button
            type="submit"
            className="btn-primary mt-6 w-full"
            disabled={registrar.isPending}
          >
            {registrar.isPending ? "Criando conta…" : "Criar conta e entrar"}
            <ArrowRight aria-hidden="true" className="h-5 w-5" />
          </button>
        </form>
      </div>
    </main>
  );
}
