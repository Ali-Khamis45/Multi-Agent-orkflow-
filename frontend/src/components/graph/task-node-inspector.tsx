"use client";

import { useWorkflowRun, useReasoningTraces } from "@/hooks/queries";
import { StatusBadge } from "@/components/shared/status-badge";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

const STAGE_ORDER = [
  "Observe",
  "Understand",
  "Think",
  "Plan",
  "RetrieveContext",
  "RetrieveMemory",
  "SelectTools",
  "Execute",
  "Reflect",
  "SelfCritique",
  "ConfidenceEvaluation",
  "PublishResult",
];

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between py-1 text-xs">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}

export function TaskNodeInspector({ workflowRunId, taskNodeId }: { workflowRunId: string; taskNodeId: string }) {
  const { data: run } = useWorkflowRun(workflowRunId);
  const { data: traces, isLoading: tracesLoading } = useReasoningTraces(taskNodeId);
  const node = run?.nodes.find((n) => n.id === taskNodeId);

  if (!node) {
    return (
      <div className="space-y-2 p-4">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-full" />
      </div>
    );
  }

  const tracesByStage = new Map(traces?.map((t) => [t.stage, t]));

  return (
    <div className="flex flex-col">
      <div className="border-b border-border p-4">
        <div className="mb-1 text-[10px] uppercase tracking-wide text-muted-foreground">Task Node</div>
        <div className="mb-2 text-sm font-semibold">{node.name}</div>
        <StatusBadge status={node.status} />

        <div className="mt-3 divide-y divide-border/60">
          <Row label="Agent" value={node.assignedAgentName ?? "—"} />
          <Row label="Task Type" value={<code className="text-[11px]">{node.taskType}</code>} />
          <Row
            label="Confidence"
            value={node.confidence !== null ? `${(node.confidence * 100).toFixed(0)}%` : "—"}
          />
          <Row label="Risk" value={node.riskLevel ?? "—"} />
          <Row label="Attempts" value={node.attemptCount} />
          <Row label="Updated" value={new Date(node.updatedAt).toLocaleTimeString()} />
        </div>
      </div>

      <div className="p-4">
        <div className="mb-2 text-[10px] uppercase tracking-wide text-muted-foreground">
          Reasoning Pipeline ({traces?.length ?? 0}/12 stages)
        </div>

        {tracesLoading && (
          <div className="space-y-1.5">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-6 w-full" />
            ))}
          </div>
        )}

        <div className="space-y-0.5">
          {STAGE_ORDER.map((stage) => {
            const trace = tracesByStage.get(stage as never);
            const done = !!trace;
            return (
              <div
                key={stage}
                className={cn(
                  "flex items-center justify-between rounded-md px-2 py-1.5 text-xs",
                  done ? "bg-secondary/40" : "opacity-40",
                )}
              >
                <div className="flex items-center gap-2">
                  <span
                    className={cn(
                      "h-1.5 w-1.5 rounded-full",
                      done ? (trace!.errorMessage ? "bg-status-error" : "bg-status-success") : "bg-status-pending",
                    )}
                  />
                  {stage}
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  {trace?.toolCalls ? <span title="Tool calls">🛠 {trace.toolCalls}</span> : null}
                  {trace?.memoryReads || trace?.memoryWrites ? (
                    <span title="Memory reads/writes">
                      🧠 {trace?.memoryReads ?? 0}/{trace?.memoryWrites ?? 0}
                    </span>
                  ) : null}
                  {trace?.tokens ? <span title="Tokens">{trace.tokens}tok</span> : null}
                  {done && <span className="tabular-nums">{trace!.durationMs}ms</span>}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
