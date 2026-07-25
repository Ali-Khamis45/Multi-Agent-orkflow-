"use client";

import { useMemo } from "react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend,
} from "recharts";
import { Skeleton } from "@/components/ui/skeleton";
import { useAgentMetrics, useReasoningTelemetry, useSupervisorSummary, useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { maxParallelism } from "@/lib/dag-layout";
import { ChartCard, CHART_ACCENT, CHART_ERROR, CHART_GRID, CHART_MUTED, CHART_SUCCESS, CHART_WARNING, TOOLTIP_STYLE } from "./chart-card";
import { CorrelationTimeline } from "./correlation-timeline";

const PIE_COLORS = [CHART_ACCENT, CHART_SUCCESS, CHART_WARNING, CHART_ERROR, CHART_MUTED, "#8884d8"];

const AXIS_PROPS = { tick: { fontSize: 10, fill: CHART_MUTED }, axisLine: { stroke: CHART_GRID }, tickLine: false };

export function TelemetryCenter() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: telemetry, isLoading } = useReasoningTelemetry(workspaceId ?? undefined);
  const { data: agentMetrics } = useAgentMetrics();
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 50 });
  const { data: supervisorSummary } = useSupervisorSummary(workspaceId ?? undefined);

  const stageDurationData = useMemo(
    () => (telemetry?.stageMetrics ?? []).map((s) => ({ stage: s.stage, ms: Math.round(s.avgDurationMs) })).sort((a, b) => b.ms - a.ms),
    [telemetry],
  );

  const tokenByStage = useMemo(
    () => (telemetry?.stageMetrics ?? []).filter((s) => s.totalTokens > 0).map((s) => ({ stage: s.stage, tokens: s.totalTokens })),
    [telemetry],
  );

  const usageByStage = useMemo(
    () => (telemetry?.stageMetrics ?? [])
      .filter((s) => s.totalToolCalls > 0 || s.totalMemoryReads > 0 || s.totalMemoryWrites > 0)
      .map((s) => ({ stage: s.stage, tools: s.totalToolCalls, memReads: s.totalMemoryReads, memWrites: s.totalMemoryWrites })),
    [telemetry],
  );

  const retriesByStage = useMemo(
    () => (telemetry?.stageMetrics ?? []).filter((s) => s.totalRetries > 0).map((s) => ({ stage: s.stage, retries: s.totalRetries })),
    [telemetry],
  );

  const confidenceHistogram = useMemo(() => {
    const buckets = Array.from({ length: 10 }, (_, i) => ({ bucket: `${i * 10}-${i * 10 + 10}%`, count: 0 }));
    for (const p of telemetry?.recentPoints ?? []) {
      if (p.confidence == null) continue;
      const idx = Math.min(9, Math.floor(p.confidence * 10));
      buckets[idx].count += 1;
    }
    return buckets;
  }, [telemetry]);

  const agentDurationData = useMemo(
    () => (agentMetrics ?? []).map((m) => ({ agent: m.agentName, ms: Math.round(m.avgStageDurationMs) })).sort((a, b) => b.ms - a.ms),
    [agentMetrics],
  );

  const agentOutcomeData = useMemo(
    () => (agentMetrics ?? []).map((m) => ({
      agent: m.agentName,
      success: Math.round(m.successRate * 100),
      failure: Math.round(m.failureRate * 100),
    })),
    [agentMetrics],
  );

  const modelUsageData = useMemo(() => {
    const totals = new Map<string, number>();
    for (const m of agentMetrics ?? []) {
      for (const [model, count] of Object.entries(m.modelUsage)) totals.set(model, (totals.get(model) ?? 0) + count);
    }
    return Array.from(totals.entries()).map(([model, count]) => ({ model, count }));
  }, [agentMetrics]);

  const decisionTypeData = useMemo(
    () => (supervisorSummary?.counts ?? []).map((c) => ({ type: c.decisionType, count: c.count })),
    [supervisorSummary],
  );

  const workflowDurationData = useMemo(() => {
    return (runs ?? [])
      .filter((r) => r.status === "Completed" || r.status === "Failed")
      .slice(0, 12)
      .map((r) => ({
        run: r.goal.length > 18 ? r.goal.slice(0, 18) + "…" : r.goal,
        seconds: Math.max(0, Math.round((new Date(r.updatedAt).getTime() - new Date(r.createdAt).getTime()) / 1000)),
      }))
      .reverse();
  }, [runs]);

  const parallelismData = useMemo(() => {
    return (runs ?? []).slice(0, 12).map((r) => ({
      run: r.goal.length > 18 ? r.goal.slice(0, 18) + "…" : r.goal,
      maxParallel: maxParallelism(r.nodes, r.edges),
    })).reverse();
  }, [runs]);

  const totalTokens = telemetry?.stageMetrics.reduce((s, m) => s + m.totalTokens, 0) ?? 0;
  const totalTraces = telemetry?.stageMetrics.reduce((s, m) => s + m.count, 0) ?? 0;
  const totalToolCalls = telemetry?.stageMetrics.reduce((s, m) => s + m.totalToolCalls, 0) ?? 0;

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-64 w-full" />)}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <MiniStat label="Reasoning stages recorded" value={totalTraces} />
        <MiniStat label="Total tokens" value={totalTokens.toLocaleString()} />
        <MiniStat label="Tool calls" value={totalToolCalls} />
        <MiniStat label="Supervisor decisions" value={supervisorSummary?.recent.length ?? 0} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard title="Reasoning Stage Duration" subtitle="Average ms per stage, across every agent">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={stageDurationData} layout="vertical" margin={{ left: 8 }}>
              <CartesianGrid stroke={CHART_GRID} horizontal={false} />
              <XAxis type="number" {...AXIS_PROPS} />
              <YAxis type="category" dataKey="stage" width={110} {...AXIS_PROPS} />
              <Tooltip {...TOOLTIP_STYLE} />
              <Bar dataKey="ms" fill={CHART_ACCENT} radius={[0, 3, 3, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Agent Duration" subtitle="Average stage latency per agent">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={agentDurationData} layout="vertical" margin={{ left: 8 }}>
              <CartesianGrid stroke={CHART_GRID} horizontal={false} />
              <XAxis type="number" {...AXIS_PROPS} />
              <YAxis type="category" dataKey="agent" width={100} {...AXIS_PROPS} />
              <Tooltip {...TOOLTIP_STYLE} />
              <Bar dataKey="ms" fill={CHART_ACCENT} radius={[0, 3, 3, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Success vs Failure Rate" subtitle="Per agent, %">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={agentOutcomeData} margin={{ left: -20 }}>
              <CartesianGrid stroke={CHART_GRID} vertical={false} />
              <XAxis dataKey="agent" {...AXIS_PROPS} interval={0} angle={-25} textAnchor="end" height={50} />
              <YAxis {...AXIS_PROPS} />
              <Tooltip {...TOOLTIP_STYLE} />
              <Legend wrapperStyle={{ fontSize: 11 }} />
              <Bar dataKey="success" name="Success %" fill={CHART_SUCCESS} radius={[3, 3, 0, 0]} />
              <Bar dataKey="failure" name="Failure %" fill={CHART_ERROR} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Confidence Distribution" subtitle="Reasoning stage confidence scores, bucketed">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={confidenceHistogram} margin={{ left: -20 }}>
              <CartesianGrid stroke={CHART_GRID} vertical={false} />
              <XAxis dataKey="bucket" {...AXIS_PROPS} interval={1} />
              <YAxis {...AXIS_PROPS} allowDecimals={false} />
              <Tooltip {...TOOLTIP_STYLE} />
              <Bar dataKey="count" fill={CHART_ACCENT} radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Tool & Memory Usage" subtitle="By reasoning stage">
          {usageByStage.length === 0 ? <EmptyChart /> : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={usageByStage} margin={{ left: -20 }}>
                <CartesianGrid stroke={CHART_GRID} vertical={false} />
                <XAxis dataKey="stage" {...AXIS_PROPS} interval={0} angle={-25} textAnchor="end" height={50} />
                <YAxis {...AXIS_PROPS} allowDecimals={false} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Legend wrapperStyle={{ fontSize: 11 }} />
                <Bar dataKey="tools" name="Tool calls" fill={CHART_ACCENT} radius={[3, 3, 0, 0]} />
                <Bar dataKey="memReads" name="Memory reads" fill={CHART_SUCCESS} radius={[3, 3, 0, 0]} />
                <Bar dataKey="memWrites" name="Memory writes" fill={CHART_WARNING} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Retries" subtitle="By reasoning stage">
          {retriesByStage.length === 0 ? <EmptyChart label="No retries recorded — every stage succeeded on the first attempt." /> : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={retriesByStage} margin={{ left: -20 }}>
                <CartesianGrid stroke={CHART_GRID} vertical={false} />
                <XAxis dataKey="stage" {...AXIS_PROPS} interval={0} angle={-25} textAnchor="end" height={50} />
                <YAxis {...AXIS_PROPS} allowDecimals={false} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Bar dataKey="retries" fill={CHART_WARNING} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Token Usage" subtitle="Total tokens by stage">
          {tokenByStage.length === 0 ? <EmptyChart /> : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={tokenByStage} layout="vertical" margin={{ left: 8 }}>
                <CartesianGrid stroke={CHART_GRID} horizontal={false} />
                <XAxis type="number" {...AXIS_PROPS} />
                <YAxis type="category" dataKey="stage" width={110} {...AXIS_PROPS} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Bar dataKey="tokens" fill={CHART_ACCENT} radius={[0, 3, 3, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Model Usage" subtitle="Invocation count by model, across all agents">
          {modelUsageData.length === 0 ? <EmptyChart /> : (
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie data={modelUsageData} dataKey="count" nameKey="model" cx="50%" cy="50%" outerRadius={80} label={{ fontSize: 10, fill: CHART_MUTED }}>
                  {modelUsageData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />)}
                </Pie>
                <Tooltip {...TOOLTIP_STYLE} />
                <Legend wrapperStyle={{ fontSize: 11 }} />
              </PieChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Supervisor Decisions" subtitle="By decision type, across the workspace">
          {decisionTypeData.length === 0 ? <EmptyChart label="No supervisor interventions yet." /> : (
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie data={decisionTypeData} dataKey="count" nameKey="type" cx="50%" cy="50%" outerRadius={80} label={{ fontSize: 10, fill: CHART_MUTED }}>
                  {decisionTypeData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />)}
                </Pie>
                <Tooltip {...TOOLTIP_STYLE} />
                <Legend wrapperStyle={{ fontSize: 11 }} />
              </PieChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Workflow Duration" subtitle="Wall-clock time, completed/failed runs">
          {workflowDurationData.length === 0 ? <EmptyChart label="No finished runs yet." /> : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={workflowDurationData} margin={{ left: -20 }}>
                <CartesianGrid stroke={CHART_GRID} vertical={false} />
                <XAxis dataKey="run" {...AXIS_PROPS} interval={0} angle={-25} textAnchor="end" height={50} />
                <YAxis {...AXIS_PROPS} unit="s" />
                <Tooltip {...TOOLTIP_STYLE} />
                <Bar dataKey="seconds" fill={CHART_ACCENT} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>

        <ChartCard title="Parallel Execution" subtitle="Max tasks dispatched together, per run (DAG column width)">
          {parallelismData.length === 0 ? <EmptyChart /> : (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={parallelismData} margin={{ left: -20 }}>
                <CartesianGrid stroke={CHART_GRID} vertical={false} />
                <XAxis dataKey="run" {...AXIS_PROPS} interval={0} angle={-25} textAnchor="end" height={50} />
                <YAxis {...AXIS_PROPS} allowDecimals={false} />
                <Tooltip {...TOOLTIP_STYLE} />
                <Bar dataKey="maxParallel" fill={CHART_ACCENT} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </ChartCard>
      </div>

      <ChartCard title="Correlation Timeline" subtitle="Every reasoning stage, in order, colored by agent">
        <CorrelationTimeline points={telemetry?.recentPoints ?? []} />
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

function EmptyChart({ label = "No data yet." }: { label?: string }) {
  return <div className="flex h-60 items-center justify-center text-xs text-muted-foreground">{label}</div>;
}
