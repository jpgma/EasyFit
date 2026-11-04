export class ApiError extends Error {
  readonly status: number;
  readonly code: string | undefined;

  constructor(
    status: number,
    code: string | undefined,
    title: string | undefined,
  ) {
    super(title ?? "Não foi possível concluir a operação.");
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

export async function getJson<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(path, {
    signal,
    headers: { Accept: "application/json" },
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      code?: string;
      title?: string;
    } | null;
    throw new ApiError(response.status, problem?.code, problem?.title);
  }

  return (await response.json()) as T;
}
