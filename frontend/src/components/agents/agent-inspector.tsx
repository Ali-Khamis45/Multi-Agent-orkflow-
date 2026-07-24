"use client";

import { useAgents, useAgentMetrics } from "@/hooks/queries";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/shared/status-badge";
import { Badge } from "@/components/ui/badge";

export function AgentInspector({ agentName }: { agentName: string }) {
  const { data: agents, isLoading } = useAgents();
  const { data: metrics } = useAgentMetrics(agentName);
  const agent = agents?.find((a) => a.name === agentName);
  const m = metrics?.[0];

  if (isLoading || !agent) {
    return (
      <div className="space-y-2 p-4">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <div className="border-b border-border p-4">
        <div className="mb-1 text-[10px] uppercase tracking-wide text-muted-foreground">Agent</div>
        <div className="mb-2 text-sm font-semibold">{agent.name}</div>
        <StatusBadge status={agent.status} />
        <p className="mt-2 text-xs text-muted-foreground">{agent.description}</p>
        <div className="mt-2 flex flex-wrap gap-1">
          {agent.skills.map((s) => (
            <Badge key={s} variant="outline" className="text-[10px]">
              {s}
            </Badge>
          ))}
        </div>
      </div>

      {m && (
        <div className="grid grid-cols-2 gap-2 p-4 text-xs">
          <Metric label="Success rate" value={`${(m.successRate * 100).toFixed(0)}%`} />
          <Metric label="Failure rate" value={`${(m.failureRate * 100).toFixed(0)}%`} />
          <Metric label="Avg confidence" value={m.avgConfidence ? `${(m.avgConfidence * 100).toFixed(0)}%` : "—"} />
          <Metric label="Avg stage latency" value={`${m.avgStageDurationMs.toFixed(0)}ms`} />
          <Metric label="Total tasks" value={m.totalTasks} />
          <Metric label="Tool calls" value={m.toolCallCount} />
        </div>
      )}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-md bg-secondary/40 p-2">
      <div className="text-[10px] text-muted-foreground">{label}</div>
      <div className="text-sm font-semibold tabular-nums">{value}</div>
    </div>
  );
}
