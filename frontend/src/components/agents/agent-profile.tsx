"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Bot, Wrench, BrainCircuit, RotateCw, Cpu, GitBranch } from "lucide-react";
import { StatusBadge } from "@/components/shared/status-badge";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useAgents, useAgentMetrics, useAgentConfidenceTrend, useWorkflowRuns, useAgentReasoningTraces } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { agentExecutions, agentCurrentTask } from "@/lib/agent-activity";
import { agentRole, initials } from "@/lib/utils";

export function AgentProfile({ agentName }: { agentName: string }) {
  const router = useRouter();
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: agents, isLoading } = useAgents();
  const { data: metrics } = useAgentMetrics(agentName);
  const { data: trend } = useAgentConfidenceTrend(agentName);
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: traces, isLoading: tracesLoading } = useAgentReasoningTraces(agentName);

  const agent = agents?.find((a) => a.name === agentName);
  const m = metrics?.[0];

  if (isLoading || !agent) {
    return (
      <div className="space-y-4 p-6">
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  const current = agentCurrentTask(runs, agentName);
  const executions = agentExecutions(runs, agentName).slice(0, 12);
  const modelEntries = m ? Object.entries(m.modelUsage) : [];
  const topModel = modelEntries.sort((a, b) => b[1] - a[1])[0]?.[0];

  return (
    <div className="mx-auto max-w-5xl space-y-5 p-6">
      <div className="flex items-center gap-3">
        <button onClick={() => router.push("/agents")} className="rounded-md p-1 text-muted-foreground hover:bg-secondary/60 hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
        </button>
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-status-running/15 text-sm font-semibold text-status-running">
          {initials(agent.name)}
        </div>
        <div className="min-w-0 flex-1">
          <h1 className="truncate text-lg font-semibold">{agent.name}</h1>
          <p className="text-xs text-muted-foreground">{agentRole(agent.name)} · v{agent.version}</p>
        </div>
        <StatusBadge status={agent.status} />
      </div>

      <p className="text-sm text-muted-foreground">{agent.description}</p>

      <div className="flex flex-wrap gap-1.5">
        {agent.skills.map((s) => <Badge key={s} variant="outline">{s}</Badge>)}
      </div>

      {current && (
        <Link
          href={`/workflows/${current.run.id}`}
          className="flex items-center gap-2 rounded-lg border border-status-running/30 bg-status-running/10 px-3 py-2 text-xs text-status-running transition-colors hover:bg-status-running/15"
        >
          <Bot className="h-3.5 w-3.5 animate-pulse shrink-0" />
          Currently running <strong>{current.node.name}</strong> on <strong>{current.run.goal}</strong>
        </Link>
      )}

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Stat label="Success rate" value={m ? `${(m.successRate * 100).toFixed(0)}%` : "—"} />
        <Stat label="Failure rate" value={m ? `${(m.failureRate * 100).toFixed(0)}%` : "—"} />
        <Stat label="Avg confidence" value={m?.avgConfidence != null ? `${(m.avgConfidence * 100).toFixed(0)}%` : "—"} />
        <Stat label="Avg stage latency" value={m ? `${m.avgStageDurationMs.toFixed(0)}ms` : "—"} />
        <Stat label="Total tasks" value={m?.totalTasks ?? "—"} />
        <Stat label="Avg attempts" value={m ? m.avgAttemptCount.toFixed(1) : "—"} icon={RotateCw} />
        <Stat label="Tool calls" value={m?.toolCallCount ?? "—"} icon={Wrench} />
        <Stat label="Memory R/W" value={m ? `${m.memoryReadCount}/${m.memoryWriteCount}` : "—"} icon={BrainCircuit} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card className="border-border/60">
          <CardHeader><CardTitle className="text-sm">Model Usage</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {modelEntries.length === 0 && <p className="text-xs text-muted-foreground">No model invocations recorded yet.</p>}
            {modelEntries.sort((a, b) => b[1] - a[1]).map(([model, count]) => (
              <div key={model} className="flex items-center gap-2 text-xs">
                <Cpu className="h-3 w-3 shrink-0 text-muted-foreground" />
                <span className="flex-1 truncate font-mono">{model}</span>
                <span className="tabular-nums text-muted-foreground">{count}</span>
                {model === topModel && <Badge variant="outline" className="text-[9px]">top</Badge>}
              </div>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border/60">
          <CardHeader><CardTitle className="text-sm">Confidence Trend</CardTitle></CardHeader>
          <CardContent>
            {!trend || trend.length === 0 ? (
              <p className="text-xs text-muted-foreground">No confidence samples yet.</p>
            ) : (
              <div className="flex h-16 items-end gap-0.5">
                {trend.map((p, i) => (
                  <div
                    key={i}
                    title={`${(p.confidence * 100).toFixed(0)}%`}
                    className="flex-1 rounded-t bg-status-running/60"
                    style={{ height: `${Math.max(4, p.confidence * 100)}%` }}
                  />
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Card className="border-border/60">
        <CardHeader><CardTitle className="text-sm">Recent Executions</CardTitle></CardHeader>
        <CardContent className="space-y-1.5">
          {executions.length === 0 && <p className="text-xs text-muted-foreground">No executions yet.</p>}
          {executions.map(({ run, node }) => (
            <Link
              key={node.id}
              href={`/workflows/${run.id}`}
              className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5 text-xs transition-colors hover:bg-secondary/40"
            >
              <div className="flex min-w-0 items-center gap-2">
                <GitBranch className="h-3 w-3 shrink-0 text-muted-foreground" />
                <span className="truncate font-medium">{node.name}</span>
                <span className="truncate text-muted-foreground">{run.goal}</span>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                {node.confidence != null && <span className="tabular-nums text-muted-foreground">{(node.confidence * 100).toFixed(0)}%</span>}
                <StatusBadge status={node.status} />
              </div>
            </Link>
          ))}
        </CardContent>
      </Card>

      <Card className="border-border/60">
        <CardHeader><CardTitle className="text-sm">Reasoning Timeline</CardTitle></CardHeader>
        <CardContent className="space-y-1">
          {tracesLoading && <Skeleton className="h-24 w-full" />}
          {!tracesLoading && (!traces || traces.length === 0) && (
            <p className="text-xs text-muted-foreground">No reasoning traces recorded yet.</p>
          )}
          {traces?.slice(0, 30).map((t) => (
            <div key={t.id} className="flex items-center gap-2 border-b border-border/40 py-1.5 text-[11px] last:border-0">
              <span className="w-16 shrink-0 text-muted-foreground/70">{new Date(t.startedAt).toLocaleTimeString()}</span>
              <span className="w-40 shrink-0 truncate font-medium">{t.stage}</span>
              <span className="flex-1 truncate text-muted-foreground">{t.modelUsed ?? "—"}</span>
              {t.tokens != null && <span className="shrink-0 tabular-nums text-muted-foreground">{t.tokens}tok</span>}
              <span className="w-14 shrink-0 text-right tabular-nums text-muted-foreground">{t.durationMs}ms</span>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

function Stat({ label, value, icon: Icon }: { label: string; value: React.ReactNode; icon?: React.ComponentType<{ className?: string }> }) {
  return (
    <div className="rounded-lg border border-border/60 bg-card/60 p-3">
      <div className="flex items-center gap-1.5 text-[10px] uppercase tracking-wide text-muted-foreground">
        {Icon && <Icon className="h-3 w-3" />}
        {label}
      </div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
    </div>
  );
}
