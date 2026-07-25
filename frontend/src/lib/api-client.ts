// Typed client for the ASP.NET Core API — the dashboard's only path to data.
// Per Phase 1.6's design goal: "communicate only with the ASP.NET API and
// SignalR. Never access Python directly." No import here ever points at the
// AI Runtime.

import type {
  Agent,
  AgentMetrics,
  Artifact,
  Checkpoint,
  ConfidencePoint,
  MemoryItem,
  MemoryOverview,
  PromptEntry,
  ReasoningTelemetry,
  ReasoningTrace,
  SupervisorDecision,
  SupervisorSummary,
  WorkflowRun,
  Workspace,
} from "./types";
import { getAuthToken } from "@/store/auth-store";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAuthToken();
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!res.ok) {
    const body = await res.text().catch(() => "");
    throw new ApiError(res.status, body || res.statusText);
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

function qs(params: Record<string, string | number | boolean | undefined>): string {
  const usp = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") usp.set(key, String(value));
  }
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export interface AuthResult {
  userId: string;
  email: string;
  name: string;
  companyType: string;
  token: string;
}

export interface CurrentUser {
  userId: string;
  email: string;
  name: string;
  companyType: string;
}

export const api = {
  // ---- Auth ----
  auth: {
    register: (email: string, password: string, name: string, companyType: string) =>
      request<AuthResult>("/api/auth/register", {
        method: "POST",
        body: JSON.stringify({ email, password, name, companyType }),
      }),
    login: (email: string, password: string) =>
      request<AuthResult>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password }),
      }),
    me: () => request<CurrentUser>("/api/auth/me"),
  },

  // ---- Workspaces ----
  workspaces: {
    list: () => request<Workspace[]>("/api/workspaces"),
    create: (name: string) =>
      request<string>("/api/workspaces", { method: "POST", body: JSON.stringify({ name }) }),
  },

  // ---- Registry ----
  agents: {
    list: () => request<Agent[]>("/api/registry/agents"),
  },

  // ---- Observability ----
  observability: {
    agentMetrics: (agentName?: string) =>
      request<AgentMetrics[]>(`/api/observability/agents/metrics${qs({ agentName })}`),
    confidenceTrend: (agentName: string, limit = 50) =>
      request<ConfidencePoint[]>(`/api/observability/agents/${agentName}/confidence-trend${qs({ limit })}`),
  },

  // ---- Workflows ----
  workflows: {
    list: (params?: { workspaceId?: string; status?: string; limit?: number }) =>
      request<WorkflowRun[]>(`/api/workflows/runs${qs(params ?? {})}`),
    get: (runId: string) => request<WorkflowRun>(`/api/workflows/runs/${runId}`),
    reschedule: (runId: string) =>
      request<void>(`/api/workflows/runs/${runId}/reschedule`, { method: "POST" }),
  },

  // ---- Intake — proxied server-to-server by the ASP.NET API to the AI
  // Runtime's Supervisor.kickoff(). The dashboard never calls the AI Runtime
  // directly (Phase 1.6 design goal). ----
  intake: {
    submit: (rawInput: string, workspaceId?: string) =>
      request<{ workflowRunId: string }>("/api/intake", {
        method: "POST",
        body: JSON.stringify({ rawInput, workspaceId }),
      }),
  },

  // ---- Checkpoints (Execution Playback) ----
  checkpoints: {
    list: (workflowRunId: string) => request<Checkpoint[]>(`/api/checkpoints${qs({ workflowRunId })}`),
  },

  // ---- Supervisor ----
  supervisor: {
    decisions: (workflowRunId: string) =>
      request<SupervisorDecision[]>(`/api/supervisor/decisions${qs({ workflowRunId })}`),
    summary: (params: { workspaceId: string; limit?: number }) =>
      request<SupervisorSummary>(`/api/supervisor/summary${qs(params)}`),
  },

  // ---- Artifacts ----
  artifacts: {
    list: (params: { workspaceId: string; workflowRunId?: string; type?: string; search?: string; limit?: number }) =>
      request<Artifact[]>(`/api/artifacts${qs(params)}`),
    get: (id: string) => request<Artifact>(`/api/artifacts/${id}`),
    versions: (id: string) => request<Artifact[]>(`/api/artifacts/${id}/versions`),
  },

  // ---- Memory ----
  memory: {
    query: (params: { workspaceId: string; layer: string; scopeRef: string; limit?: number }) =>
      request<MemoryItem[]>(`/api/memory${qs(params)}`),
    overview: (params: { workspaceId: string; layer?: string; limit?: number }) =>
      request<MemoryOverview>(`/api/memory/overview${qs(params)}`),
  },

  // ---- Reasoning ----
  reasoning: {
    traces: (taskNodeId: string) => request<ReasoningTrace[]>(`/api/reasoning/traces/${taskNodeId}`),
    telemetry: (params: { workspaceId: string; pointsLimit?: number }) =>
      request<ReasoningTelemetry>(`/api/reasoning/telemetry${qs(params)}`),
    agentTraces: (agentName: string, limit = 100) =>
      request<ReasoningTrace[]>(`/api/reasoning/agents/${encodeURIComponent(agentName)}/traces${qs({ limit })}`),
  },

  // ---- Prompt registry — proxied by the ASP.NET API (GET /api/prompts) from
  // the AI Runtime's file-based registry. ----
  prompts: {
    list: () => request<PromptEntry[]>("/api/prompts"),
  },
};

export { ApiError };
