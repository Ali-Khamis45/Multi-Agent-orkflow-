"use client";

import { motion } from "motion/react";
import { GitBranch, RotateCcw, Users, MessagesSquare, Route } from "lucide-react";
import { useSupervisorDecisions } from "@/hooks/queries";
import { Skeleton } from "@/components/ui/skeleton";
import type { SupervisorDecisionType } from "@/lib/types";

const DECISION_ICON: Record<SupervisorDecisionType, React.ComponentType<{ className?: string }>> = {
  Replan: GitBranch,
  Retry: RotateCcw,
  Reassign: Users,
  Debate: MessagesSquare,
  StrategySelection: Route,
};

export function SupervisorTimeline({ workflowRunId }: { workflowRunId: string }) {
  const { data: decisions, isLoading } = useSupervisorDecisions(workflowRunId);

  if (isLoading) {
    return (
      <div className="space-y-2 p-6">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (!decisions || decisions.length === 0) {
    return (
      <p className="p-6 text-center text-sm text-muted-foreground">
        No supervisor interventions yet — the Supervisor Brain only acts on replans, retries, reassignment, or debate.
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-3 p-6">
      {decisions.map((d, i) => {
        const Icon = DECISION_ICON[d.decisionType] ?? Route;
        return (
          <motion.div
            key={d.id}
            initial={{ opacity: 0, x: -8 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.15, delay: i * 0.02 }}
            className="flex gap-3 rounded-lg border border-border/60 bg-card/60 p-3"
          >
            <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-status-running/15 text-status-running">
              <Icon className="h-3.5 w-3.5" />
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-semibold">{d.decisionType}</span>
                <span className="shrink-0 text-[10px] text-muted-foreground">
                  {new Date(d.createdAt).toLocaleTimeString()}
                </span>
              </div>
              <p className="mt-1 text-xs text-muted-foreground">{d.rationale}</p>
              <span className="mt-1 inline-block text-[10px] tabular-nums text-muted-foreground">
                confidence {(d.confidence * 100).toFixed(0)}%
              </span>
            </div>
          </motion.div>
        );
      })}
    </div>
  );
}
