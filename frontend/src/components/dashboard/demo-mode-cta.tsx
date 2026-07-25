"use client";

import { useRouter } from "next/navigation";
import { Sparkles, PlayCircle, Loader2 } from "lucide-react";
import { motion } from "motion/react";
import { Button } from "@/components/ui/button";
import { useWorkflowRuns, useSubmitIntake } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { toast } from "sonner";

export const DEMO_GOAL = "Build a Task Management SaaS";

/**
 * Portfolio Demo entry point. This never fabricates data — it either jumps
 * to a real completed run of the canonical demo goal, or submits that goal
 * to the real intake pipeline (which runs end-to-end in well under a minute
 * on the Multi-Model Router's mock fallback, so no API keys are required)
 * and follows the run live. Either way, what a visitor sees is the actual
 * multi-agent system, not a canned recording.
 */
export function DemoModeCta() {
  const router = useRouter();
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs, isLoading } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const submit = useSubmitIntake();

  const existingDemo = runs?.find((r) => r.goal === DEMO_GOAL && r.status === "Completed");

  const runDemo = () => {
    if (existingDemo) {
      router.push(`/workflows/${existingDemo.id}`);
      return;
    }
    submit.mutate(
      { rawInput: DEMO_GOAL, workspaceId: workspaceId ?? undefined },
      {
        onSuccess: (result) => {
          toast.success("Portfolio demo started", { description: "Watch the full pipeline run live." });
          router.push(`/workflows/${result.workflowRunId}`);
        },
        onError: (err) => toast.error("Failed to start demo", { description: String(err) }),
      },
    );
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: -4 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-start justify-between gap-3 rounded-xl border border-status-running/30 bg-gradient-to-r from-status-running/10 to-transparent p-4 sm:flex-row sm:items-center"
    >
      <div className="flex items-center gap-3">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-status-running/15 text-status-running">
          <Sparkles className="h-4 w-4" />
        </div>
        <div>
          <div className="text-sm font-semibold">
            {existingDemo ? "Portfolio demo ready" : "See it work — one click"}
          </div>
          <p className="text-xs text-muted-foreground">
            {existingDemo
              ? `A completed "${DEMO_GOAL}" run is available — jump straight to the live DAG, reasoning, and artifacts.`
              : `Runs the real pipeline end-to-end on "${DEMO_GOAL}" — no API keys needed, completes in under a minute.`}
          </p>
        </div>
      </div>
      <Button size="sm" onClick={runDemo} disabled={isLoading || submit.isPending} className="shrink-0">
        {submit.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <PlayCircle className="h-3.5 w-3.5" />}
        {existingDemo ? "View demo" : "Run demo"}
      </Button>
    </motion.div>
  );
}
