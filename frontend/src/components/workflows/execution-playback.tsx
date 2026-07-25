"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { Play, Pause, SkipBack, SkipForward, Rewind } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Slider } from "@/components/ui/slider";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { useCheckpoints, useSupervisorDecisions, useArtifacts } from "@/hooks/queries";
import { ExecutionGraph } from "@/components/graph/execution-graph";
import type { CheckpointSnapshot, WorkflowRun, TaskNode } from "@/lib/types";

function parseSnapshot(json: string): CheckpointSnapshot | null {
  try {
    return JSON.parse(json) as CheckpointSnapshot;
  } catch {
    return null;
  }
}

export function ExecutionPlayback({ run }: { run: WorkflowRun }) {
  const { data: checkpoints, isLoading } = useCheckpoints(run.id);
  const { data: decisions } = useSupervisorDecisions(run.id);
  const { data: artifacts } = useArtifacts({ workspaceId: run.workspaceId, workflowRunId: run.id });

  const [index, setIndex] = useState(0);
  const [playing, setPlaying] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const snapshots = useMemo(
    () => (checkpoints ?? []).map((c) => ({ checkpoint: c, snapshot: parseSnapshot(c.snapshotJson) })).filter((s) => s.snapshot),
    [checkpoints],
  );

  useEffect(() => {
    if (!playing) {
      if (timerRef.current) clearInterval(timerRef.current);
      return;
    }
    timerRef.current = setInterval(() => {
      setIndex((i) => {
        if (i >= snapshots.length - 1) {
          setPlaying(false);
          return i;
        }
        return i + 1;
      });
    }, 900);
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [playing, snapshots.length]);

  if (isLoading) {
    return <div className="space-y-3 p-6"><Skeleton className="h-8 w-64" /><Skeleton className="h-96 w-full" /></div>;
  }

  if (snapshots.length === 0) {
    return (
      <p className="flex h-full items-center justify-center p-6 text-center text-sm text-muted-foreground">
        No execution snapshots recorded for this run yet — checkpoints are written after every scheduling pass.
      </p>
    );
  }

  const current = snapshots[Math.min(index, snapshots.length - 1)];
  const currentTime = new Date(current.checkpoint.createdAt).getTime();

  const syntheticNodes: TaskNode[] = current.snapshot!.Nodes.map((n) => ({
    id: n.Id,
    name: n.Name,
    taskType: n.TaskType,
    status: n.Status,
    assignedAgentName: n.AssignedAgentName,
    confidence: n.Confidence,
    riskLevel: n.RiskLevel,
    attemptCount: n.AttemptCount,
    createdAt: current.checkpoint.createdAt,
    updatedAt: current.checkpoint.createdAt,
  }));

  const playbackRun: WorkflowRun = {
    ...run,
    status: current.snapshot!.Status as WorkflowRun["status"],
    nodes: syntheticNodes,
    edges: current.snapshot!.Edges.map((e) => ({ predecessorNodeId: e.PredecessorNodeId, successorNodeId: e.SuccessorNodeId })),
    updatedAt: current.checkpoint.createdAt,
  };

  const decisionsSoFar = (decisions ?? []).filter((d) => new Date(d.createdAt).getTime() <= currentTime);
  const artifactsSoFar = (artifacts ?? []).filter((a) => new Date(a.createdAt).getTime() <= currentTime);

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="min-h-0 flex-1">
        <ExecutionGraph run={playbackRun} />
      </div>

      <div className="shrink-0 space-y-2 border-t border-border bg-card/60 p-3">
        <div className="flex items-center gap-3">
          <Button size="icon" variant="ghost" className="h-7 w-7" aria-label="Restart" onClick={() => { setIndex(0); setPlaying(false); }}>
            <Rewind className="h-3.5 w-3.5" />
          </Button>
          <Button size="icon" variant="ghost" className="h-7 w-7" aria-label="Step back" onClick={() => setIndex((i) => Math.max(0, i - 1))}>
            <SkipBack className="h-3.5 w-3.5" />
          </Button>
          <Button
            size="icon"
            variant="outline"
            className="h-8 w-8"
            aria-label={playing ? "Pause" : "Play"}
            onClick={() => setPlaying((p) => !p)}
          >
            {playing ? <Pause className="h-3.5 w-3.5" /> : <Play className="h-3.5 w-3.5" />}
          </Button>
          <Button size="icon" variant="ghost" className="h-7 w-7" aria-label="Step forward" onClick={() => setIndex((i) => Math.min(snapshots.length - 1, i + 1))}>
            <SkipForward className="h-3.5 w-3.5" />
          </Button>

          <div className="flex-1 px-2">
            <Slider
              min={0}
              max={snapshots.length - 1}
              step={1}
              value={[index]}
              onValueChange={(v) => {
                const next = Array.isArray(v) ? v[0] : v;
                setPlaying(false);
                setIndex(next);
              }}
            />
          </div>

          <span className="w-28 shrink-0 text-right text-[11px] tabular-nums text-muted-foreground">
            {index + 1} / {snapshots.length}
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-2 text-[11px] text-muted-foreground">
          <Badge variant="outline" className="text-[10px]">{current.checkpoint.label}</Badge>
          <span>{new Date(current.checkpoint.createdAt).toLocaleString()}</span>
          <span>·</span>
          <span>{decisionsSoFar.length} supervisor decision{decisionsSoFar.length === 1 ? "" : "s"} so far</span>
          <span>·</span>
          <span>{artifactsSoFar.length} artifact{artifactsSoFar.length === 1 ? "" : "s"} produced so far</span>
        </div>
      </div>
    </div>
  );
}
