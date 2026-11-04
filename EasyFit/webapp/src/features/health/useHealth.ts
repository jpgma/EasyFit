import { useQuery } from "@tanstack/react-query";
import { getJson } from "@/services/api";

interface HealthResponse {
  status: string;
}

export function useHealth() {
  return useQuery({
    queryKey: ["health"],
    queryFn: () => getJson<HealthResponse>("/health"),
    retry: false,
  });
}
