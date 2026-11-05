import { zodResolver } from "@hookform/resolvers/zod";
import { Users } from "lucide-react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { PageHeader } from "@/components/PageHeader";
import { TextField } from "@/components/TextField";
import { useCriarGrupo } from "@/features/grupos";
import { getErrorMessage } from "@/services/api";

// Os limites acompanham Features/Grupos/Rules.cs; o backend é quem decide.
const schema = z.object({
  nome: z
    .string()
    .trim()
    .min(1, "Digite o nome do grupo")
    .max(80, "Use no máximo 80 caracteres"),
  fusoHorario: z.string().min(1, "Escolha o fuso horário"),
});

type FormData = z.infer<typeof schema>;

const fusoPadrao = "America/Sao_Paulo";

function getFusoDoNavegador(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || fusoPadrao;
}

function getFusos(fusoAtual: string): string[] {
  const fusos = Intl.supportedValuesOf("timeZone");
  return fusos.includes(fusoAtual) ? fusos : [fusoAtual, ...fusos];
}

export function CriarGrupoPage() {
  const criar = useCriarGrupo();
  const navigate = useNavigate();
  const fusoAtual = getFusoDoNavegador();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { nome: "", fusoHorario: fusoAtual },
  });

  return (
    <div className="mx-auto w-full max-w-xl">
      <PageHeader title="Criar grupo" back="/" />
      <p className="mb-6 text-sm text-balance text-muted">
        Um espaço privado para até 10 amigos. Depois de criar, você poderá
        convidar as pessoas.
      </p>

      <form
        noValidate
        onSubmit={(event) =>
          void handleSubmit((data) =>
            criar.mutate(data, {
              onSuccess: ({ id }) =>
                void navigate(`/grupos/${id}`, { replace: true }),
            }),
          )(event)
        }
        className="space-y-4"
      >
        <TextField
          label="Nome do grupo"
          icon={Users}
          type="text"
          autoComplete="off"
          placeholder="Ex.: Amigos do treino"
          error={errors.nome?.message}
          {...register("nome")}
        />

        <div>
          <label htmlFor="fuso-horario" className="input-label">
            Fuso horário
          </label>
          <select
            id="fuso-horario"
            className="input-field"
            aria-describedby="fuso-horario-ajuda"
            {...register("fusoHorario")}
          >
            {getFusos(fusoAtual).map((fuso) => (
              <option key={fuso} value={fuso}>
                {fuso.replaceAll("_", " ")}
              </option>
            ))}
          </select>
          <p id="fuso-horario-ajuda" className="mt-1.5 text-sm text-muted">
            Define quando cada semana do grupo começa e termina. Não poderá ser
            alterado depois.
          </p>
        </div>

        {criar.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(criar.error)}
          </p>
        )}

        <button
          type="submit"
          className="btn-primary mt-6 w-full"
          disabled={criar.isPending}
        >
          {criar.isPending ? "Criando…" : "Criar grupo"}
        </button>
      </form>
    </div>
  );
}
