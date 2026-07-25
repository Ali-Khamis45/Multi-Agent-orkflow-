"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";

// ---- Workspaces ----
export const useWorkspaces = () => useQuery({ queryKey: ["workspaces"], queryFn: api.workspaces.list });

export const useCreateWorkspace = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => api.workspaces.create(name),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["workspaces"] }),
  });
};

// ---- Agents ----
export const useAgents = () =>
  useQuery({ queryKey: ["agents"], queryFn: api.agents.list, refetchInterval: 10_000 });

export const useAgentMetrics = (agentName?: string) =>
  useQuery({
    queryKey: ["agent-metrics", agentName ?? "all"],
    queryFn: () => api.observability.agentMetrics(agentName),
  });

export const useAgentConfidenceTrend = (agentName: string) =>
  useQuery({
    queryKey: ["agent-confidence-trend", agentName],
    queryFn: () => api.observability.confidenceTrend(agentName),
    enabled: !!agentName,
  });

// ---- Workflows ----
export const useWorkflowRuns = (params?: { workspaceId?: string; status?: string; limit?: number }) =>
  useQuery({
    queryKey: ["workflow-runs", params],
    queryFn: () => api.workflows.list(params),
    refetchInterval: 5_000,
  });

export const useWorkflowRun = (runId: string | undefined) =>
  useQuery({
    queryKey: ["workflow-run", runId],
    queryFn: () => api.workflows.get(runId!),
    enabled: !!runId,
  });

export const useSubmitIntake = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ rawInput, workspaceId }: { rawInput: string; workspaceId?: string }) =>
      api.intake.submit(rawInput, workspaceId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["workflow-runs"] }),
  });
};

// ---- Checkpoints ----
export const useCheckpoints = (workflowRunId: string | undefined) =>
  useQuery({
    queryKey: ["checkpoints", workflowRunId],
    queryFn: () => api.checkpoints.list(workflowRunId!),
    enabled: !!workflowRunId,
  });

// ---- Supervisor ----
export const useSupervisorDecisions = (workflowRunId: string | undefined) =>
  useQuery({
    queryKey: ["supervisor-decisions", workflowRunId],
    queryFn: () => api.supervisor.decisions(workflowRunId!),
    enabled: !!workflowRunId,
  });

export const useSupervisorSummary = (workspaceId: string | undefined) =>
  useQuery({
    queryKey: ["supervisor-summary", workspaceId],
    queryFn: () => api.supervisor.summary({ workspaceId: workspaceId! }),
    enabled: !!workspaceId,
    refetchInterval: 15_000,
  });

// ---- Artifacts ----
export const useArtifacts = (params: { workspaceId: string; workflowRunId?: string; type?: string; search?: string }) =>
  useQuery({
    queryKey: ["artifacts", params],
    queryFn: () => api.artifacts.list(params),
    enabled: !!params.workspaceId,
  });

export const useArtifact = (id: string | undefined) =>
  useQuery({ queryKey: ["artifact", id], queryFn: () => api.artifacts.get(id!), enabled: !!id });

export const useArtifactVersions = (id: string | undefined) =>
  useQuery({
    queryKey: ["artifact-versions", id],
    queryFn: () => api.artifacts.versions(id!),
    enabled: !!id,
  });

// ---- Memory ----
export const useMemory = (params: { workspaceId: string; layer: string; scopeRef: string } | undefined) =>
  useQuery({
    queryKey: ["memory", params],
    queryFn: () => api.memory.query(params!),
    enabled: !!params,
  });

export const useMemoryOverview = (params: { workspaceId?: string; layer?: string } | undefined) =>
  useQuery({
    queryKey: ["memory-overview", params],
    queryFn: () => api.memory.overview({ workspaceId: params!.workspaceId!, layer: params?.layer }),
    enabled: !!params?.workspaceId,
    refetchInterval: 15_000,
  });

// ---- Reasoning ----
export const useReasoningTraces = (taskNodeId: string | undefined) =>
  useQuery({
    queryKey: ["reasoning-traces", taskNodeId],
    queryFn: () => api.reasoning.traces(taskNodeId!),
    enabled: !!taskNodeId,
  });

export const useReasoningTelemetry = (workspaceId: string | undefined) =>
  useQuery({
    queryKey: ["reasoning-telemetry", workspaceId],
    queryFn: () => api.reasoning.telemetry({ workspaceId: workspaceId! }),
    enabled: !!workspaceId,
    refetchInterval: 15_000,
  });

export const useAgentReasoningTraces = (agentName: string | undefined) =>
  useQuery({
    queryKey: ["agent-reasoning-traces", agentName],
    queryFn: () => api.reasoning.agentTraces(agentName!),
    enabled: !!agentName,
  });

// ---- Prompts ----
export const usePrompts = () => useQuery({ queryKey: ["prompts"], queryFn: api.prompts.list });
