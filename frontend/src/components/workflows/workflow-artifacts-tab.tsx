"use client";

import { FileText } from "lucide-react";
import { useArtifacts } from "@/hooks/queries";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { useInspectorStore } from "@/store/inspector-store";
import type { WorkflowRun } from "@/lib/types";

export function WorkflowArtifactsTab({ run }: { run: WorkflowRun }) {
  const { data: artifacts, isLoading } = useArtifacts({ workspaceId: run.workspaceId, workflowRunId: run.id });
  const openInspector = useInspectorStore((s) => s.open);

  if (isLoading) {
    return (
      <div className="space-y-2 p-6">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (!artifacts || artifacts.length === 0) {
    return <p className="p-6 text-center text-sm text-muted-foreground">No artifacts produced yet.</p>;
  }

  return (
    <div className="mx-auto max-w-3xl space-y-2 p-6">
      {artifacts.map((a) => (
        <button
          key={a.id}
          onClick={() => openInspector({ kind: "artifact", artifactId: a.id })}
          className="flex w-full items-center gap-3 rounded-lg border border-border/60 bg-card/60 p-3 text-left transition-colors hover:bg-secondary/40"
        >
          <FileText className="h-4 w-4 shrink-0 text-muted-foreground" />
          <span className="min-w-0 flex-1 truncate text-xs font-medium">{a.name}</span>
          <Badge variant="outline" className="text-[10px]">
            {a.type}
          </Badge>
          <Badge variant="outline" className="text-[10px]">
            v{a.version}
          </Badge>
          <span className="text-[10px] text-muted-foreground">{a.ownerAgent}</span>
        </button>
      ))}
    </div>
  );
}
