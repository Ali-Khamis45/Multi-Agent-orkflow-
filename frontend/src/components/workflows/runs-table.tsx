"use client";

import Link from "next/link";
import { formatDistanceToNow } from "date-fns";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { StatusBadge } from "@/components/shared/status-badge";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";
import { useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";

export function RunsTable() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs, isLoading } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-10 w-full" />
        ))}
      </div>
    );
  }

  if (!runs || runs.length === 0) {
    return <p className="py-10 text-center text-sm text-muted-foreground">No workflow runs yet — start one above.</p>;
  }

  return (
    <Table>
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead>Goal</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Progress</TableHead>
          <TableHead>Started</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {runs.map((run) => {
          const total = run.nodes.length;
          const completed = run.nodes.filter((n) => n.status === "Completed").length;
          return (
            <TableRow key={run.id} className="cursor-pointer">
              <TableCell className="max-w-md">
                <Link href={`/workflows/${run.id}`} className="block truncate font-medium hover:underline">
                  {run.goal}
                </Link>
                <span className="font-mono text-[10px] text-muted-foreground">{run.id.slice(0, 8)}</span>
              </TableCell>
              <TableCell>
                <StatusBadge status={run.status} />
              </TableCell>
              <TableCell className="w-40">
                <div className="flex items-center gap-2">
                  <Progress value={total ? (completed / total) * 100 : 0} className="h-1.5 w-24" />
                  <span className="text-[11px] tabular-nums text-muted-foreground">
                    {completed}/{total}
                  </span>
                </div>
              </TableCell>
              <TableCell className="text-xs text-muted-foreground">
                {formatDistanceToNow(new Date(run.createdAt), { addSuffix: true })}
              </TableCell>
            </TableRow>
          );
        })}
      </TableBody>
    </Table>
  );
}
