"use client";

import { useMemo, useState } from "react";
import { Search, Download, FileText, FileJson, FileCode, FileTerminal, Database, Image as ImageIcon, Workflow } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useArtifacts, useArtifactVersions, useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { ArtifactPreview } from "@/components/artifacts/artifact-preview";
import { ArtifactDiff } from "@/components/artifacts/artifact-diff";
import { ArtifactVersions } from "@/components/artifacts/artifact-versions";
import { downloadArtifact } from "@/lib/artifact-lang";
import type { ArtifactType } from "@/lib/types";
import { cn } from "@/lib/utils";

const TYPE_ICON: Record<ArtifactType, React.ComponentType<{ className?: string }>> = {
  Code: FileCode,
  Markdown: FileText,
  Json: FileJson,
  Test: FileTerminal,
  Dockerfile: Database,
  Sql: Database,
  Image: ImageIcon,
  Diagram: Workflow,
};

const TYPES: (ArtifactType | "All")[] = ["All", "Markdown", "Code", "Json", "Test", "Dockerfile", "Sql", "Image", "Diagram"];

export function ArtifactsExplorer({ defaultSearch = "" }: { defaultSearch?: string } = {}) {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const [search, setSearch] = useState(defaultSearch);
  const [type, setType] = useState<string>("All");
  const [runId, setRunId] = useState<string>("All");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: artifacts, isLoading } = useArtifacts({
    workspaceId: workspaceId ?? "",
    workflowRunId: runId === "All" ? undefined : runId,
    type: type === "All" ? undefined : type,
    search: search || undefined,
  });
  // No selection yet (or the selected artifact isn't in the current filtered
  // list) falls back to the first result — derived at render time rather than
  // via an effect, so there's no extra render pass syncing state to state.
  const effectiveSelectedId =
    selectedId && artifacts?.some((a) => a.id === selectedId) ? selectedId : (artifacts?.[0]?.id ?? null);

  const { data: versions } = useArtifactVersions(effectiveSelectedId ?? undefined);

  const selected = useMemo(
    () => versions?.find((v) => v.id === effectiveSelectedId) ?? artifacts?.find((a) => a.id === effectiveSelectedId),
    [versions, artifacts, effectiveSelectedId],
  );

  return (
    <div className="grid h-full min-h-0 grid-cols-1 gap-4 lg:grid-cols-[320px_1fr]">
      <div className="flex min-h-0 flex-col gap-2">
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search artifacts…"
            className="h-8 bg-secondary/30 pl-8 text-xs"
          />
        </div>
        <div className="flex gap-2">
          <Select value={type} onValueChange={(v) => v && setType(v)}>
            <SelectTrigger className="h-7 flex-1 text-xs"><SelectValue placeholder="Type" /></SelectTrigger>
            <SelectContent>
              {TYPES.map((t) => <SelectItem key={t} value={t}>{t}</SelectItem>)}
            </SelectContent>
          </Select>
          <Select value={runId} onValueChange={(v) => v && setRunId(v)}>
            <SelectTrigger className="h-7 flex-1 text-xs"><SelectValue placeholder="Run" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="All">All runs</SelectItem>
              {runs?.map((r) => <SelectItem key={r.id} value={r.id}>{r.goal.slice(0, 30)}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>

        <div className="max-h-72 min-h-0 flex-1 overflow-y-auto rounded-lg border border-border/60 lg:max-h-none">
          {isLoading && (
            <div className="space-y-1 p-2">
              {Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-9 w-full" />)}
            </div>
          )}
          {!isLoading && (!artifacts || artifacts.length === 0) && (
            <p className="p-6 text-center text-xs text-muted-foreground">No artifacts match these filters.</p>
          )}
          {artifacts?.map((a) => {
            const Icon = TYPE_ICON[a.type];
            return (
              <button
                key={a.id}
                onClick={() => setSelectedId(a.id)}
                className={cn(
                  "flex w-full items-center gap-2 border-b border-border/40 px-2.5 py-2 text-left text-xs transition-colors last:border-0 hover:bg-secondary/40",
                  a.id === effectiveSelectedId && "bg-secondary/60",
                )}
              >
                <Icon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                <span className="min-w-0 flex-1 truncate font-medium">{a.name}</span>
                <span className="shrink-0 text-[10px] text-muted-foreground">v{a.version}</span>
              </button>
            );
          })}
        </div>
      </div>

      <div className="flex min-h-0 flex-col rounded-lg border border-border/60 bg-card/40">
        {!selected ? (
          <p className="flex flex-1 items-center justify-center text-sm text-muted-foreground">
            Select an artifact to preview it.
          </p>
        ) : (
          <>
            <div className="flex items-center gap-2 border-b border-border/60 p-3">
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-semibold">{selected.name}</div>
                <div className="mt-0.5 flex items-center gap-1.5 text-[10px] text-muted-foreground">
                  <Badge variant="outline" className="text-[10px]">{selected.type}</Badge>
                  <span>v{selected.version}</span>
                  <span>·</span>
                  <span>{selected.ownerAgent}</span>
                </div>
              </div>
              <Button
                size="sm"
                variant="outline"
                onClick={() => downloadArtifact(selected.name, selected.content ?? "")}
              >
                <Download className="h-3.5 w-3.5" /> Download
              </Button>
            </div>

            <Tabs defaultValue="preview" className="flex min-h-0 flex-1 flex-col">
              <TabsList className="mx-3 mt-2 w-fit shrink-0">
                <TabsTrigger value="preview">Preview</TabsTrigger>
                <TabsTrigger value="versions">Versions ({versions?.length ?? 1})</TabsTrigger>
                <TabsTrigger value="diff">Diff</TabsTrigger>
              </TabsList>
              <TabsContent value="preview" className="min-h-0 flex-1 overflow-y-auto p-3">
                <ArtifactPreview artifact={selected} />
              </TabsContent>
              <TabsContent value="versions" className="min-h-0 flex-1 overflow-y-auto p-3">
                <ArtifactVersions
                  versions={versions ?? [selected]}
                  activeId={selected.id}
                  onSelect={setSelectedId}
                />
              </TabsContent>
              <TabsContent value="diff" className="min-h-0 flex-1 overflow-y-auto p-3">
                <ArtifactDiff versions={versions ?? [selected]} />
              </TabsContent>
            </Tabs>
          </>
        )}
      </div>
    </div>
  );
}
