"use client";

import { useMemo } from "react";
import Link from "next/link";
import { GitBranch, RotateCcw, Users, MessagesSquare, Route, ArrowRight } from "lucide-react";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend,
} from "recharts";
import { Skeleton } from "@/components/ui/skeleton";
import { useSupervisorSummary, useWorkflowRuns, useAgentMetrics } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { ChartCard, CHART_ACCENT, CHART_GRID, CHART_MUTED, TOOLTIP_STYLE } from "@/components/telemetry/chart-card";
import type { SupervisorDecisionType } from "@/lib/types";

const PIE_COLORS = ["var(--color-status-running)", "var(--color-status-success)", "var(--color-status-warning)", "var(--color-status-error)", "var(--color-muted-foreground)"];

const DECISION_ICON: Record<SupervisorDecisionType, React.ComponentType<{ className?: string }>> = {
  Replan: GitBranch, Retry: RotateCcw, Reassign: Users, Debate: MessagesSquare, StrategySelection: Route,
};

const AXIS_PROPS = { tick: { fontSize: 10, fill: CHART_MUTED }, axisLine: { stroke: CHART_GRID }, tickLine: false };

export function SupervisorBrain() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: summary, isLoading } = useSupervisorSummary(workspaceId ?? undefined);
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: agentMetrics } = useAgentMetrics();

  const runById = useMemo(() => new Map((runs ?? []).map((r) => [r.id, r])), [runs]);

  const confidenceEvolution = useMemo(
    () => [...(summary?.recent ?? [])]
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
      .map((d, i) => ({ i, confidence: Math.round(d.confidence * 100), type: d.decisionType })),
    [summary],
  );

  const decisionTypeData = useMemo(
    () => (summary?.counts ?? []).map((c) => ({ type: c.decisionType, count: c.count })),
    [summary],
  );

  const retries = useMemo(() => (summary?.recent ?? []).filter((d) => d.decisionType === "Retry"), [summary]);

  const assignmentData = useMemo(
    () => [...(agentMetrics ?? [])].sort((a, b) => b.totalTasks - a.totalTasks).map((m) => ({ agent: m.agentName, tasks: m.totalTasks })),
    [agentMetrics],
  );

  if (isLoading) {
    return <div className="space-y-4"><Skeleton className="h-32 w-full" /><Skeleton className="h-64 w-full" /></div>;
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <MiniStat label="Total decisions" value={summary?.recent.length ?? 0} />
        <MiniStat label="Retries" value={retries.length} />
        <MiniStat label="Decision types" value={decisionTypeData.length} />
        <MiniStat
          label="Avg confidence"
          value={summary?.recent.length ? `${Math.round((summary.recent.reduce((s, d) => s + d.confidence, 0) / summary.recent.length) * 100)}%` : "—"}
        />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard title="Confidence Evolution" subtitle="Supervisor decision confidence, in order made">
          {confidenceEvolution.length === 0 ? (
            <div className="flex h-56 items-center justify-center text-xs text-muted-foreground">No decisions yet.</div>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <LineChart data={confidenceEvolution} margin={{ left: -20 }}>
                <CartesianGrid stroke={CHART_GRID} vertical={false} />
                <XAxis dataKey="i" {...AXIS_PROPS} tickFormatter={(v) => `#${v + 1}`} />
                <YAxis {...AXIS_PROPS} domain={[0, 100]} unit="%" />
                <Tooltip {...TOOLTIP_STYLE} formatter={(v) => [`${v}%`, "confidence"]} labelFormatter={(v) => `Decision #${Number(v) + 1}`} />
                <Line type="monotone" dataKey="confidence" stroke={CHART_ACCENT} strokeWidth={2} dot={{ r: 3 }} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Decision Types" subtitle="Replan, Retry, Reassign, Debate, Strategy Selection">
          {decisionTypeData.length === 0 ? (
            <div className="flex h-56 items-center justify-center text-xs text-muted-foreground">No decisions yet.</div>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <PieChart>
                <Pie data={decisionTypeData} dataKey="count" nameKey="type" cx="50%" cy="50%" outerRadius={75} label={{ fontSize: 10, fill: CHART_MUTED }}>
                  {decisionTypeData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />)}
                </Pie>
                <Tooltip {...TOOLTIP_STYLE} />
                <Legend wrapperStyle={{ fontSize: 11 }} />
              </PieChart>
            </ResponsiveContainer>
          )}
        </ChartCard>
      </div>

      <ChartCard title="Agent Assignment" subtitle="Tasks the Supervisor has dispatched to each agent">
        {assignmentData.length === 0 ? (
          <div className="flex h-24 items-center justify-center text-xs text-muted-foreground">No assignments yet.</div>
        ) : (
          <div className="space-y-1.5">
            {assignmentData.map((a) => {
              const max = Math.max(1, ...assignmentData.map((x) => x.tasks));
              return (
                <div key={a.agent} className="flex items-center gap-2 text-xs">
                  <span className="w-32 shrink-0 truncate">{a.agent}</span>
                  <div className="h-2 flex-1 overflow-hidden rounded-full bg-secondary/40">
                    <div className="h-full rounded-full bg-status-running" style={{ width: `${(a.tasks / max) * 100}%` }} />
                  </div>
                  <span className="w-6 shrink-0 text-right tabular-nums text-muted-foreground">{a.tasks}</span>
                </div>
              );
            })}
          </div>
        )}
      </ChartCard>

      <ChartCard title="Decision History" subtitle="Every strategy, replan, retry, and reassignment across the workspace">
        <div className="space-y-2">
          {(!summary || summary.recent.length === 0) && (
            <p className="py-8 text-center text-xs text-muted-foreground">No supervisor interventions recorded yet.</p>
          )}
          {summary?.recent.map((d) => {
            const Icon = DECISION_ICON[d.decisionType] ?? Route;
            const run = runById.get(d.workflowRunId);
            return (
              <div key={d.id} className="flex gap-3 rounded-lg border border-border/60 bg-card/40 p-3">
                <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-status-running/15 text-status-running">
                  <Icon className="h-3.5 w-3.5" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-semibold">{d.decisionType}</span>
                    <span className="shrink-0 text-[10px] text-muted-foreground">{new Date(d.createdAt).toLocaleString()}</span>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">{d.rationale}</p>
                  <div className="mt-1.5 flex items-center gap-3">
                    <span className="text-[10px] tabular-nums text-muted-foreground">confidence {(d.confidence * 100).toFixed(0)}%</span>
                    {run && (
                      <Link href={`/workflows/${run.id}`} className="flex items-center gap-1 text-[10px] text-status-running hover:underline">
                        {run.goal} <ArrowRight className="h-2.5 w-2.5" />
                      </Link>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </ChartCard>
    </div>
  );
}

function MiniStat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-border/60 bg-card/60 p-3">
      <div className="text-[10px] uppercase tracking-wide text-muted-foreground">{label}</div>
      <div className="mt-1 text-lg font-semibold tabular-nums">{value}</div>
    </div>
  );
}
