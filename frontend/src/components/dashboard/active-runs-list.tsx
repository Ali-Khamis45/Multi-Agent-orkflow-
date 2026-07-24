"use client";

import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { StatusBadge } from "@/components/shared/status-badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";

export function ActiveRunsList() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs, isLoading } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 20 });

  const active = runs?.filter((r) => r.status === "Running" || r.status === "WaitingApproval" || r.status === "Planning");

  return (
    <Card className="border-border/60">
      <CardHeader>
        <CardTitle className="text-sm">What&apos;s Running</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {isLoading && (
          <>
            <Skeleton className="h-14 w-full" />
            <Skeleton className="h-14 w-full" />
          </>
        )}

        {!isLoading && (!active || active.length === 0) && (
          <p className="py-6 text-center text-xs text-muted-foreground">
            Nothing running right now. Start a workflow to see it live here.
          </p>
        )}

        {active?.map((run) => {
          const total = run.nodes.length;
          const completed = run.nodes.filter((n) => n.status === "Completed").length;
          return (
            <Link
              key={run.id}
              href={`/workflows/${run.id}`}
              className="block rounded-lg border border-border/60 p-3 transition-colors hover:bg-secondary/40"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-sm font-medium">{run.goal}</span>
                <StatusBadge status={run.status} />
              </div>
              <div className="mt-2 flex items-center gap-2">
                <Progress value={total ? (completed / total) * 100 : 0} className="h-1.5" />
                <span className="shrink-0 text-[11px] tabular-nums text-muted-foreground">
                  {completed}/{total}
                </span>
              </div>
            </Link>
          );
        })}
      </CardContent>
    </Card>
  );
}
