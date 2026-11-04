import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Camera, Clock, Dumbbell, FileText } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router";
import { z } from "zod";
import { SplashScreen } from "@/components/SplashScreen";
import { TextField } from "@/components/TextField";
import {
  comprimirImagem,
  tipoAtividadeLabels,
  tipoComprovacaoLabels,
  tiposAtividade,
  useCriarAtividade,
  type TipoAtividade,
  type TipoComprovacao,
} from "@/features/atividades";
import { useGrupo } from "@/features/grupos";
import { getErrorMessage } from "@/services/api";

const schema = z.object({
  tipo: z.enum(tiposAtividade as [string, ...string[]]),
  duracaoMinutos: z
    .number({ error: "Informe a duração" })
    .int("Use um número inteiro")
    .min(1, "Use pelo menos 1 minuto")
    .max(1440, "Use no máximo 1440 minutos"),
  startedAt: z.string().min(1, "Informe quando você treinou"),
  descricao: z.string().trim().max(500, "Use no máximo 500 caracteres"),
  tipoComprovacao: z.enum(["RegistroManual", "Fotografia", "CapturaDeTela"] as [
    TipoComprovacao,
    ...TipoComprovacao[],
  ]),
});

type FormData = z.infer<typeof schema>;

function toLocalInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function RegistrarAtividadePage() {
  const { id: grupoId = "" } = useParams();
  const navigate = useNavigate();
  const grupo = useGrupo(grupoId);
  const criar = useCriarAtividade(grupoId);
  const [arquivos, setArquivos] = useState<File[]>([]);
  const [enviandoEvidencias, setEnviandoEvidencias] = useState(false);
  const [erroUpload, setErroUpload] = useState<string | null>(null);
  const [mostrarEvidencias, setMostrarEvidencias] = useState(true);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      tipo: "Corrida",
      duracaoMinutos: 60,
      startedAt: toLocalInputValue(new Date()),
      descricao: "",
      tipoComprovacao: "Fotografia",
    },
  });

  if (grupo.isPending) return <SplashScreen />;

  if (grupo.isError || !grupo.data) {
    return (
      <main className="mx-auto min-h-dvh max-w-2xl px-5 py-6">
        <p role="alert" className="form-error">
          {getErrorMessage(grupo.error)}
        </p>
        <Link to="/" className="btn-secondary mt-4">
          Voltar
        </Link>
      </main>
    );
  }

  return (
    <main className="mx-auto min-h-dvh max-w-2xl animate-fade-in px-5 py-6">
      <Link to={`/grupos/${grupoId}`} className="btn-ghost -ml-2">
        <ArrowLeft aria-hidden="true" className="h-5 w-5" />
        {grupo.data.nome}
      </Link>

      <h1 className="mt-4 text-xl font-bold">Registrar atividade</h1>

      <form
        noValidate
        className="mt-6 space-y-4"
        onSubmit={(event) =>
          void handleSubmit(async (data) => {
            setErroUpload(null);
            const exigeEvidencia = data.tipoComprovacao !== "RegistroManual";
            if (exigeEvidencia && arquivos.length === 0) {
              setErroUpload("Escolha pelo menos uma foto para comprovar.");
              return;
            }

            const atividade = await criar.mutateAsync({
              tipo: data.tipo as TipoAtividade,
              duracaoSegundos: data.duracaoMinutos * 60,
              startedAt: new Date(data.startedAt).toISOString(),
              descricao: data.descricao,
              tipoComprovacao: data.tipoComprovacao,
            });

            if (exigeEvidencia) {
              setEnviandoEvidencias(true);
              try {
                for (const arquivo of arquivos.slice(0, 3)) {
                  const comprimido = await comprimirImagem(arquivo);
                  const form = new FormData();
                  form.append("arquivo", comprimido);
                  const response = await fetch(
                    `/api/atividades/${atividade.id}/evidencias`,
                    { method: "POST", body: form },
                  );
                  if (!response.ok) {
                    const problem = (await response
                      .json()
                      .catch(() => null)) as {
                      title?: string;
                    } | null;
                    throw new Error(
                      problem?.title ?? "Não foi possível enviar a evidência.",
                    );
                  }
                }
              } catch (error) {
                setErroUpload(getErrorMessage(error));
                setEnviandoEvidencias(false);
                return;
              }
              setEnviandoEvidencias(false);
            }

            void navigate(`/atividades/${atividade.id}`, { replace: true });
          })(event)
        }
      >
        <label className="block">
          <span className="label">Tipo de atividade</span>
          <select className="input w-full" {...register("tipo")}>
            {tiposAtividade.map((tipo) => (
              <option key={tipo} value={tipo}>
                {tipoAtividadeLabels[tipo]}
              </option>
            ))}
          </select>
        </label>

        <TextField
          label="Duração (minutos)"
          icon={Clock}
          type="number"
          min={1}
          error={errors.duracaoMinutos?.message}
          {...register("duracaoMinutos", { valueAsNumber: true })}
        />

        <TextField
          label="Quando você treinou"
          icon={Dumbbell}
          type="datetime-local"
          error={errors.startedAt?.message}
          {...register("startedAt")}
        />

        <TextField
          label="Descrição (opcional)"
          icon={FileText}
          type="text"
          autoComplete="off"
          error={errors.descricao?.message}
          {...register("descricao")}
        />

        <label className="block">
          <span className="label">Comprovação</span>
          <select
            className="input w-full"
            {...register("tipoComprovacao", {
              onChange: (event) => {
                setMostrarEvidencias(event.target.value !== "RegistroManual");
              },
            })}
          >
            {(
              Object.entries(tipoComprovacaoLabels) as [
                TipoComprovacao,
                string,
              ][]
            ).map(([valor, rotulo]) => (
              <option key={valor} value={valor}>
                {rotulo}
              </option>
            ))}
          </select>
        </label>

        {mostrarEvidencias && (
          <div className="card space-y-3 p-4">
            <p className="text-sm font-semibold">Fotos (até 3)</p>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              multiple
              className="text-sm"
              onChange={(event) => {
                const lista = event.target.files
                  ? Array.from(event.target.files).slice(0, 3)
                  : [];
                setArquivos(lista);
              }}
            />
            {arquivos.length > 0 && (
              <p className="text-2xs text-muted">
                {arquivos.length}{" "}
                {arquivos.length === 1
                  ? "arquivo selecionado"
                  : "arquivos selecionados"}
              </p>
            )}
            <p className="flex items-center gap-2 text-2xs text-muted">
              <Camera aria-hidden="true" className="h-4 w-4 shrink-0" />
              As imagens são comprimidas antes do envio.
            </p>
          </div>
        )}

        {criar.isError && (
          <p role="alert" className="form-error">
            {getErrorMessage(criar.error)}
          </p>
        )}
        {erroUpload && (
          <p role="alert" className="form-error">
            {erroUpload}
          </p>
        )}

        <button
          type="submit"
          className="btn-primary w-full"
          disabled={criar.isPending || enviandoEvidencias}
        >
          {criar.isPending || enviandoEvidencias
            ? "Enviando…"
            : "Registrar atividade"}
        </button>
      </form>
    </main>
  );
}
