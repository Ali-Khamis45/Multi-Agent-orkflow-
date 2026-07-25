"use client";

import Link from "next/link";
import { Activity, CheckCircle2, XCircle, Bot, ArrowRight } from "lucide-react";
import { StatCard } from "@/components/dashboard/stat-card";
import { ActiveRunsList } from "@/components/dashboard/active-runs-list";
import { AgentFleet } from "@/components/dashboard/agent-fleet";
import { DemoModeCta } from "@/components/dashboard/demo-mode-cta";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/shared/status-badge";
import { useWorkflowRuns, useAgents, useAgentMetrics } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";

function isToday(iso: string): boolean {
  const d = new Date(iso);
  const now = new Date();
  return d.toDateString() === now.toDateString();
}

export default function DashboardPage() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: agents } = useAgents();
  const { data: metrics } = useAgentMetrics();

  const activeCount = runs?.filter((r) => ["Running", "WaitingApproval", "Planning"].includes(r.status)).length ?? 0;
  const todayRuns = runs?.filter((r) => isToday(r.createdAt)) ?? [];
  const completedToday = todayRuns.filter((r) => r.status === "Completed").length;
  const failedToday = todayRuns.filter((r) => r.status === "Failed").length;
  const availableAgents = agents?.filter((a) => a.status === "Available").length ?? 0;

  const avgLatency = metrics && metrics.length > 0 ? metrics.reduce((sum, m) => sum + m.avgStageDurationMs, 0) / metrics.length : null;

  const modelUsage = new Map<string, number>();
  metrics?.forEach((m) => {
    Object.entries(m.modelUsage).forEach(([model, count]) => {
      modelUsage.set(model, (modelUsage.get(model) ?? 0) + count);
    });
  });

  const recent = [...(runs ?? [])].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 6);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-lg font-semibold tracking-tight">Dashboard</h1>
          <p className="text-xs text-muted-foreground">Autonomous AI Software Engineering Company — live status</p>
        </div>
        <Button size="sm" nativeButton={false} render={<Link href="/workflows" />}>
          New workflow <ArrowRight className="h-3.5 w-3.5" />
        </Button>
      </div>

      <DemoModeCta />

      <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
        <StatCard label="Active executions" value={activeCount} icon={Activity} tone="running" />
        <StatCard label="Completed today" value={completedToday} icon={CheckCircle2} tone="success" />
        <StatCard label="Failed today" value={failedToday} icon={XCircle} tone={failedToday > 0 ? "error" : "default"} />
        <StatCard
          label="Agents available"
          value={`${availableAgents}/${agents?.length ?? 0}`}
          icon={Bot}
          tone={availableAgents > 0 ? "success" : "warning"}
        />
        <StatCard
          label="Avg stage latency"
          value={avgLatency !== null ? `${avgLatency.toFixed(0)}ms` : "—"}
          icon={Activity}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <ActiveRunsList />
        </div>
        <AgentFleet />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="border-border/60">
          <CardHeader>
            <CardTitle className="text-sm">Recent Activity</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1.5">
            {recent.length === 0 && <p className="py-6 text-center text-xs text-muted-foreground">No runs yet.</p>}
            {recent.map((run) => (
              <Link
                key={run.id}
                href={`/workflows/${run.id}`}
                className="flex items-center justify-between rounded-md px-2 py-1.5 text-xs transition-colors hover:bg-secondary/40"
              >
                <span className="truncate">{run.goal}</span>
                <StatusBadge status={run.status} />
              </Link>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border/60">
          <CardHeader>
            <CardTitle className="text-sm">Model Usage</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {modelUsage.size === 0 && <p className="py-6 text-center text-xs text-muted-foreground">No model invocations recorded yet.</p>}
            {[...modelUsage.entries()]
              .sort((a, b) => b[1] - a[1])
              .map(([model, count]) => {
                const max = Math.max(...modelUsage.values());
                return (
                  <div key={model} className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-mono">{model}</span>
                      <span className="tabular-nums text-muted-foreground">{count}</span>
                    </div>
                    <div className="h-1.5 w-full rounded-full bg-secondary">
                      <div
                        className="h-1.5 rounded-full bg-status-running"
                        style={{ width: `${(count / max) * 100}%` }}
                      />
                    </div>
                  </div>
                );
              })}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
