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

// ---- Supervisor ----
export const useSupervisorDecisions = (workflowRunId: string | undefined) =>
  useQuery({
    queryKey: ["supervisor-decisions", workflowRunId],
    queryFn: () => api.supervisor.decisions(workflowRunId!),
    enabled: !!workflowRunId,
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

// ---- Reasoning ----
export const useReasoningTraces = (taskNodeId: string | undefined) =>
  useQuery({
    queryKey: ["reasoning-traces", taskNodeId],
    queryFn: () => api.reasoning.traces(taskNodeId!),
    enabled: !!taskNodeId,
  });

// ---- Prompts ----
export const usePrompts = () => useQuery({ queryKey: ["prompts"], queryFn: api.prompts.list });
