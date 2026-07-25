"use client";

import { Download, FileJson, FileText, Workflow as WorkflowIcon, Activity, GitBranch } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useSupervisorDecisions, useArtifacts, useReasoningTelemetry } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import {
  exportArtifactsJson,
  exportExecutionSummaryJson,
  exportExecutionSummaryMarkdown,
  exportGraphJson,
  exportReasoningTraceJson,
  exportTelemetryJson,
} from "@/lib/export";
import type { WorkflowRun } from "@/lib/types";

export function WorkflowExportMenu({ run }: { run: WorkflowRun }) {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: decisions } = useSupervisorDecisions(run.id);
  const { data: artifacts } = useArtifacts({ workspaceId: run.workspaceId, workflowRunId: run.id });
  const { data: telemetry } = useReasoningTelemetry(workspaceId ?? undefined);

  const runPoints = (telemetry?.recentPoints ?? []).filter((p) => p.workflowRunId === run.id);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={<Button variant="outline" size="sm" />}
      >
        <Download className="h-3.5 w-3.5" /> Export
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="text-xs text-muted-foreground">Execution Summary</DropdownMenuLabel>
          <DropdownMenuItem onClick={() => exportExecutionSummaryJson(run, decisions ?? [], artifacts ?? [])}>
            <FileJson className="h-3.5 w-3.5" /> as JSON
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => exportExecutionSummaryMarkdown(run, decisions ?? [], artifacts ?? [])}>
            <FileText className="h-3.5 w-3.5" /> as Markdown
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={() => exportArtifactsJson(run, artifacts ?? [])}>
            <WorkflowIcon className="h-3.5 w-3.5" /> Artifacts (JSON)
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => exportGraphJson(run)}>
            <GitBranch className="h-3.5 w-3.5" /> Workflow Graph (JSON)
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => exportReasoningTraceJson(run, runPoints)}>
            <Activity className="h-3.5 w-3.5" /> Reasoning Trace (JSON)
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => exportTelemetryJson(run, runPoints)}>
            <Activity className="h-3.5 w-3.5" /> Telemetry (JSON)
          </DropdownMenuItem>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
