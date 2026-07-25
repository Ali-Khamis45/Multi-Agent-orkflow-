"use client";

import { useMemo } from "react";
import { motion } from "motion/react";
import { Info } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useAgents, useAgentMetrics, useArtifacts, useReasoningTelemetry, useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { computeHealthReport } from "@/lib/health-score";
import { cn } from "@/lib/utils";

function scoreColor(score: number | null): string {
  if (score == null) return "text-muted-foreground";
  if (score >= 80) return "text-status-success";
  if (score >= 50) return "text-status-warning";
  return "text-status-error";
}

function ringColor(score: number | null): string {
  if (score == null) return "var(--color-muted-foreground)";
  if (score >= 80) return "var(--color-status-success)";
  if (score >= 50) return "var(--color-status-warning)";
  return "var(--color-status-error)";
}

function ScoreRing({ score, size = 96 }: { score: number | null; size?: number }) {
  const radius = (size - 10) / 2;
  const circumference = 2 * Math.PI * radius;
  const pct = score ?? 0;
  const offset = circumference - (pct / 100) * circumference;

  return (
    <div className="relative shrink-0" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={radius} stroke="var(--color-border)" strokeWidth={7} fill="none" />
        {score != null && (
          <motion.circle
            cx={size / 2}
            cy={size / 2}
            r={radius}
            stroke={ringColor(score)}
            strokeWidth={7}
            fill="none"
            strokeLinecap="round"
            strokeDasharray={circumference}
            initial={{ strokeDashoffset: circumference }}
            animate={{ strokeDashoffset: offset }}
            transition={{ duration: 0.8, ease: "easeOut" }}
          />
        )}
      </svg>
      <div className="absolute inset-0 flex items-center justify-center">
        <span className={cn("text-xl font-semibold tabular-nums", scoreColor(score))}>
          {score != null ? Math.round(score) : "—"}
        </span>
      </div>
    </div>
  );
}

export function ProjectHealth() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs, isLoading: runsLoading } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 200 });
  const { data: agentMetrics } = useAgentMetrics();
  const { data: agents } = useAgents();
  const { data: artifacts } = useArtifacts({ workspaceId: workspaceId ?? "" });
  const { data: telemetry } = useReasoningTelemetry(workspaceId ?? undefined);

  const report = useMemo(
    () => computeHealthReport(runs ?? [], agentMetrics ?? [], agents ?? [], artifacts ?? [], telemetry),
    [runs, agentMetrics, agents, artifacts, telemetry],
  );

  if (runsLoading) {
    return <div className="grid grid-cols-2 gap-4 md:grid-cols-3"><Skeleton className="h-40 w-full" /><Skeleton className="h-40 w-full" /><Skeleton className="h-40 w-full" /></div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col items-center gap-3 rounded-xl border border-border/60 bg-card/60 p-6 sm:flex-row sm:justify-center sm:gap-6">
        <ScoreRing score={report.overall} size={128} />
        <div className="text-center sm:text-left">
          <div className="text-sm font-semibold">Overall Engineering Score</div>
          <p className="mt-1 max-w-sm text-xs text-muted-foreground">
            Average of every measured category below. Categories with no real signal yet (Security, Maintainability) are excluded rather than guessed at.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        {report.metrics.map((m, i) => (
          <motion.div
            key={m.key}
            initial={{ opacity: 0, y: 6 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.15, delay: i * 0.03 }}
            className="flex flex-col items-center gap-2 rounded-xl border border-border/60 bg-card/60 p-4 text-center"
          >
            <ScoreRing score={m.score} size={72} />
            <div className="flex items-center gap-1">
              <span className="text-xs font-medium">{m.label}</span>
              <Tooltip>
                <TooltipTrigger render={<button type="button" aria-label={`${m.label} methodology`} />}>
                  <Info className="h-3 w-3 text-muted-foreground" />
                </TooltipTrigger>
                <TooltipContent className="max-w-56 text-[11px]">{m.methodology}</TooltipContent>
              </Tooltip>
            </div>
            <span className="text-[10px] text-muted-foreground">{m.detail}</span>
          </motion.div>
        ))}
      </div>
    </div>
  );
}
