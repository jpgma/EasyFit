import { zodResolver } from "@hookform/resolvers/zod";
import { KeyRound } from "lucide-react";
import { useForm } from "react-hook-form";
import { Link, useLocation } from "react-router";
import { z } from "zod";
import { Logo } from "@/components/BrandMark";
import { TextField } from "@/components/TextField";
import { useRedefinirSenha } from "@/features/autenticacao";
import { getErrorMessage } from "@/services/api";

const schema = z
  .object({
    novaSenha: z
      .string()
      .min(8, "Use pelo menos 8 caracteres")
      .max(128, "Use até 128 caracteres"),
    confirmacao: z.string(),
  })
  .refine(({ novaSenha, confirmacao }) => novaSenha === confirmacao, {
    message: "As senhas não são iguais",
    path: ["confirmacao"],
  });

type FormData = z.infer<typeof schema>;

export function RedefinirSenhaPage() {
  const { hash } = useLocation();
  const token = hash.slice(1);
  const redefinir = useRedefinirSenha();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <main className="mx-auto flex min-h-dvh max-w-sm flex-col justify-center px-6 py-10">
      <Logo />
      <h1 className="mt-10 text-3xl font-bold">Nova senha</h1>

      {redefinir.isSuccess ? (
        <>
          <p className="mt-2 text-muted">
            Senha alterada. Entre de novo para continuar.
          </p>
          <Link to="/entrar" className="btn-primary mt-8 w-full">
            Entrar
          </Link>
        </>
      ) : !token ? (
        <p role="alert" className="form-error mt-6">
          Abra o link completo que você recebeu de quem administra o EasyFit.
        </p>
      ) : (
        <form
          noValidate
          className="mt-8 space-y-4"
          onSubmit={(event) =>
            void handleSubmit(({ novaSenha }) =>
              redefinir.mutate({ token, novaSenha }),
            )(event)
          }
        >
          <TextField
            label="Nova senha"
            icon={KeyRound}
            type="password"
            autoComplete="new-password"
            error={errors.novaSenha?.message}
            {...register("novaSenha")}
          />
          <TextField
            label="Confirme a nova senha"
            icon={KeyRound}
            type="password"
            autoComplete="new-password"
            error={errors.confirmacao?.message}
            {...register("confirmacao")}
          />
          {redefinir.isError && (
            <p role="alert" className="form-error">
              {getErrorMessage(redefinir.error)}
            </p>
          )}
          <button
            type="submit"
            className="btn-primary w-full"
            disabled={redefinir.isPending}
          >
            {redefinir.isPending ? "Salvando…" : "Salvar nova senha"}
          </button>
        </form>
      )}
    </main>
  );
}
