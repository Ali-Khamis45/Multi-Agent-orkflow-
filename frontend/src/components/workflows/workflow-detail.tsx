"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Workflow as WorkflowIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { StatusBadge } from "@/components/shared/status-badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useWorkflowRun } from "@/hooks/queries";
import { useLiveWorkflow } from "@/hooks/use-live-workflow";
import { ExecutionGraph } from "@/components/graph/execution-graph";
import { SupervisorTimeline } from "@/components/workflows/supervisor-timeline";
import { WorkflowArtifactsTab } from "@/components/workflows/workflow-artifacts-tab";
import { ExecutionPlayback } from "@/components/workflows/execution-playback";
import { WorkflowExportMenu } from "@/components/workflows/workflow-export-menu";

export function WorkflowDetail({ runId }: { runId: string }) {
  const router = useRouter();
  const { data: run, isLoading } = useWorkflowRun(runId);
  useLiveWorkflow(runId);

  useEffect(() => {
    document.title = run ? `${run.goal} — Mission Control` : "Workflow Run — Mission Control";
  }, [run]);

  if (isLoading || !run) {
    return (
      <div className="space-y-4 p-6">
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  const total = run.nodes.length;
  const completed = run.nodes.filter((n) => n.status === "Completed").length;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex shrink-0 items-center gap-3 border-b border-border px-6 py-4">
        <Button variant="ghost" size="icon" className="h-7 w-7" aria-label="Back to workflow runs" onClick={() => router.push("/workflows")}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <WorkflowIcon className="h-4 w-4 text-muted-foreground" />
        <div className="min-w-0 flex-1">
          <h1 className="truncate text-sm font-semibold">{run.goal}</h1>
          <span className="font-mono text-[10px] text-muted-foreground">{run.id}</span>
        </div>
        <span className="text-[11px] tabular-nums text-muted-foreground">
          {completed}/{total} tasks
        </span>
        <StatusBadge status={run.status} />
        <WorkflowExportMenu run={run} />
      </div>

      <Tabs defaultValue="graph" className="flex min-h-0 flex-1 flex-col gap-0">
        <TabsList className="mx-6 mt-3 w-fit shrink-0">
          <TabsTrigger value="graph">Execution Graph</TabsTrigger>
          <TabsTrigger value="playback">Playback</TabsTrigger>
          <TabsTrigger value="supervisor">Supervisor</TabsTrigger>
          <TabsTrigger value="artifacts">Artifacts</TabsTrigger>
        </TabsList>

        <TabsContent value="graph" className="min-h-0 flex-1">
          <ExecutionGraph run={run} />
        </TabsContent>
        <TabsContent value="playback" className="min-h-0 flex-1">
          <ExecutionPlayback run={run} />
        </TabsContent>
        <TabsContent value="supervisor" className="min-h-0 flex-1 overflow-y-auto">
          <SupervisorTimeline workflowRunId={run.id} />
        </TabsContent>
        <TabsContent value="artifacts" className="min-h-0 flex-1 overflow-y-auto">
          <WorkflowArtifactsTab run={run} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
