"use client";

import { useState } from "react";
import { motion } from "motion/react";
import { BrainCircuit, Boxes, MessagesSquare, FolderKanban, Infinity as InfinityIcon, Link2, GitCommitVertical, Network, Sparkles } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useMemoryOverview } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { useInspectorStore } from "@/store/inspector-store";
import { cn } from "@/lib/utils";
import type { MemoryLayer } from "@/lib/types";

const LAYERS: { key: MemoryLayer; icon: React.ComponentType<{ className?: string }>; blurb: string }[] = [
  { key: "Working", icon: Boxes, blurb: "Per-task scratch space, cleared when the task completes." },
  { key: "Conversation", icon: MessagesSquare, blurb: "Intent-session Q&A and clarifications." },
  { key: "Workflow", icon: GitCommitVertical, blurb: "Facts shared across one workflow run's tasks." },
  { key: "Project", icon: FolderKanban, blurb: "Durable, cross-run knowledge about the workspace." },
  { key: "LongTerm", icon: InfinityIcon, blurb: "Retained indefinitely across projects." },
];

export function MemoryInspector() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const [activeLayer, setActiveLayer] = useState<string | null>(null);
  const { data, isLoading } = useMemoryOverview({ workspaceId: workspaceId ?? undefined, layer: activeLayer ?? undefined });
  const openInspector = useInspectorStore((s) => s.open);

  const countFor = (layer: string) => data?.layerCounts.find((c) => c.layer === layer)?.count ?? 0;
  const total = data?.layerCounts.reduce((sum, c) => sum + c.count, 0) ?? 0;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">
        <button
          onClick={() => setActiveLayer(null)}
          className={cn(
            "rounded-lg border p-3 text-left transition-colors",
            activeLayer === null ? "border-status-running/50 bg-status-running/10" : "border-border/60 bg-card/60 hover:bg-secondary/30",
          )}
        >
          <div className="flex items-center gap-1.5 text-xs font-medium"><BrainCircuit className="h-3.5 w-3.5" /> All layers</div>
          <div className="mt-1 text-lg font-semibold tabular-nums">{total}</div>
        </button>
        {LAYERS.map(({ key, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setActiveLayer(key)}
            className={cn(
              "rounded-lg border p-3 text-left transition-colors",
              activeLayer === key ? "border-status-running/50 bg-status-running/10" : "border-border/60 bg-card/60 hover:bg-secondary/30",
            )}
          >
            <div className="flex items-center gap-1.5 text-xs font-medium"><Icon className="h-3.5 w-3.5" /> {key}</div>
            <div className="mt-1 text-lg font-semibold tabular-nums">{countFor(key)}</div>
          </button>
        ))}
      </div>

      <div className="rounded-lg border border-border/60 bg-muted/20 p-3 text-xs text-muted-foreground">
        {LAYERS.find((l) => l.key === activeLayer)?.blurb ?? "Every layer, most recent first."}
      </div>

      <div className="space-y-2">
        {isLoading && Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}
        {!isLoading && (!data || data.items.length === 0) && (
          <p className="py-12 text-center text-sm text-muted-foreground">No memory written to this layer yet.</p>
        )}
        {data?.items.map((item, i) => (
          <motion.div
            key={item.id}
            initial={{ opacity: 0, y: 4 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.12, delay: Math.min(i, 20) * 0.01 }}
            className="rounded-lg border border-border/60 bg-card/60 p-3"
          >
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="text-[10px]">{item.layer}</Badge>
              <Badge variant="outline" className="text-[10px]">{item.kind}</Badge>
              {item.version > 1 && (
                <span className="flex items-center gap-1 text-[10px] text-status-warning">
                  <GitCommitVertical className="h-3 w-3" /> v{item.version}
                </span>
              )}
              {item.supersededById && (
                <span className="text-[10px] text-muted-foreground">superseded</span>
              )}
              <span className="ml-auto text-[10px] text-muted-foreground">{new Date(item.createdAt).toLocaleString()}</span>
            </div>
            <p className="mt-1.5 line-clamp-3 text-xs text-muted-foreground">{item.content}</p>
            <div className="mt-1.5 flex items-center gap-3 text-[10px] text-muted-foreground">
              <span className="font-mono">scope {item.scopeRef.slice(0, 8)}</span>
              {item.sourceArtifactId && (
                <button
                  onClick={() => openInspector({ kind: "artifact", artifactId: item.sourceArtifactId! })}
                  className="flex items-center gap-1 text-status-running hover:underline"
                >
                  <Link2 className="h-3 w-3" /> source artifact
                </button>
              )}
            </div>
          </motion.div>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Card className="border-dashed border-border/60 bg-transparent">
          <CardHeader><CardTitle className="flex items-center gap-2 text-sm text-muted-foreground"><Network className="h-4 w-4" /> Knowledge Graph</CardTitle></CardHeader>
          <CardContent><p className="text-xs text-muted-foreground">Entity/relationship graph over memory items — planned for Phase 3 (semantic retrieval), not yet built.</p></CardContent>
        </Card>
        <Card className="border-dashed border-border/60 bg-transparent">
          <CardHeader><CardTitle className="flex items-center gap-2 text-sm text-muted-foreground"><Sparkles className="h-4 w-4" /> Vector Memory</CardTitle></CardHeader>
          <CardContent><p className="text-xs text-muted-foreground">Embedding-similarity retrieval — the schema already carries a Score field for this; ranking logic is planned for Phase 3.</p></CardContent>
        </Card>
      </div>
    </div>
  );
}
