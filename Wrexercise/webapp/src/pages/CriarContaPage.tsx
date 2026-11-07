import { zodResolver } from "@hookform/resolvers/zod";
import { ChevronLeft, Lock, Mail, Ticket, User } from "lucide-react";
import { useForm } from "react-hook-form";
import { Link, useLocation } from "react-router";
import { z } from "zod";
import { Logo } from "@/components/BrandMark";
import { TextField } from "@/components/TextField";
import { useRegistrar } from "@/features/autenticacao";
import { extractConviteToken } from "@/features/convites";
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
    convite: z.string().optional(),
  })
  .refine((data) => data.senha === data.confirmarSenha, {
    message: "As senhas não coincidem",
    path: ["confirmarSenha"],
  });

type FormData = z.infer<typeof schema>;

export function CriarContaPage() {
  const registrar = useRegistrar();
  const { state } = useLocation() as {
    state: { conviteToken?: string } | null;
  };
  const conviteDoLink = state?.conviteToken;
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <main className="mx-auto min-h-dvh w-full max-w-sm px-6 py-6">
      <div className="flex items-center justify-between">
        <Link
          to={conviteDoLink ? `/convite#${conviteDoLink}` : "/entrar"}
          aria-label="Voltar"
          className="grid size-10 place-items-center rounded-full bg-surface shadow-card tap"
        >
          <ChevronLeft aria-hidden="true" className="size-5" />
        </Link>
        <Logo />
        <div className="size-10" />
      </div>

      <div className="animate-slide-up">
        <h1 className="mt-8 text-3xl font-bold">Criar sua conta</h1>
        <p className="mt-2 text-muted">
          O Wrexercise funciona por convite. Seus dados ficam visíveis só para o
          seu grupo.
        </p>

        <form
          noValidate
          onSubmit={(event) =>
            void handleSubmit(({ nomeExibicao, email, senha, convite }) =>
              registrar.mutate({
                nomeExibicao,
                email,
                senha,
                conviteToken:
                  conviteDoLink ??
                  (extractConviteToken(convite ?? "") || undefined),
              }),
            )(event)
          }
          className="mt-7 space-y-4"
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
          {!conviteDoLink && (
            <TextField
              label="Convite"
              icon={Ticket}
              type="text"
              autoComplete="off"
              placeholder="Cole aqui o link que você recebeu"
              {...register("convite")}
            />
          )}

          {registrar.isError && (
            <p role="alert" className="form-error">
              {getErrorMessage(registrar.error)}
            </p>
          )}

          <button
            type="submit"
            className="btn-primary w-full"
            disabled={registrar.isPending}
          >
            {registrar.isPending ? "Criando conta…" : "Criar conta"}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-muted">
          Já tem conta?{" "}
          <Link to="/entrar" className="font-semibold text-link">
            Entrar
          </Link>
        </p>
      </div>
    </main>
  );
}
