import { zodResolver } from "@hookform/resolvers/zod";
import { Lock, Mail, User } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Logo } from "@/components/BrandMark";
import { TextField } from "@/components/TextField";
import { useConfigurarPrimeiroUsuario } from "@/features/autenticacao";
import { getErrorMessage } from "@/services/api";

// Os limites acompanham Features/Autenticacao/ValidacaoConta.cs; o backend é quem decide.
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

/**
 * Cria a primeira conta do app. O guard só deixa chegar aqui enquanto não há usuários;
 * ao concluir, a sessão aberta leva a pessoa para a tela inicial.
 */
export function ConfiguracaoInicialPage() {
  const configurar = useConfigurarPrimeiroUsuario();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <main className="mx-auto min-h-dvh w-full max-w-sm px-6 py-6">
      <div className="flex justify-center">
        <Logo />
      </div>

      <div className="animate-slide-up">
        <h1 className="mt-8 text-3xl uppercase">Bem-vindo ao Wrexercise</h1>
        <p className="mt-2 text-muted">
          Vamos criar a primeira conta. Depois, você cria o grupo e convida as
          outras pessoas por um link.
        </p>

        <form
          noValidate
          onSubmit={(event) =>
            void handleSubmit(({ nomeExibicao, email, senha }) =>
              configurar.mutate({ nomeExibicao, email, senha }),
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

          {configurar.isError && (
            <p role="alert" className="form-error">
              {getErrorMessage(configurar.error)}
            </p>
          )}

          <button
            type="submit"
            className="btn-primary w-full"
            disabled={configurar.isPending}
          >
            {configurar.isPending ? "Criando conta…" : "Criar conta"}
          </button>
        </form>
      </div>
    </main>
  );
}
