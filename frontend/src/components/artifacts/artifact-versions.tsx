"use client";

import { GitCommitHorizontal } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/shared/status-badge";
import type { Artifact } from "@/lib/types";

export function ArtifactVersions({ versions, activeId, onSelect }: {
  versions: Artifact[];
  activeId: string;
  onSelect: (id: string) => void;
}) {
  return (
    <div className="relative space-y-0.5 py-1 pl-4">
      <div className="absolute top-2 bottom-2 left-[9px] w-px bg-border" />
      {versions.map((v) => (
        <button
          key={v.id}
          onClick={() => onSelect(v.id)}
          className={`relative flex w-full items-center gap-3 rounded-md px-2 py-2 text-left text-xs transition-colors hover:bg-secondary/40 ${
            v.id === activeId ? "bg-secondary/50" : ""
          }`}
        >
          <GitCommitHorizontal className={`absolute -left-4 h-3.5 w-3.5 rounded-full bg-background ${v.id === activeId ? "text-status-running" : "text-muted-foreground"}`} />
          <span className="font-mono font-medium">v{v.version}</span>
          <span className="flex-1 truncate text-muted-foreground">{v.ownerAgent}</span>
          <StatusBadge status={v.status} />
          <span className="w-36 shrink-0 text-right text-[10px] text-muted-foreground">
            {new Date(v.createdAt).toLocaleString()}
          </span>
        </button>
      ))}
    </div>
  );
}

export function ArtifactTypeBadge({ type }: { type: string }) {
  return <Badge variant="outline" className="text-[10px]">{type}</Badge>;
}
