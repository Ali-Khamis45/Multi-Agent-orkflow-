"use client";

import { useArtifact, useArtifactVersions } from "@/hooks/queries";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/shared/status-badge";
import { Badge } from "@/components/ui/badge";

export function ArtifactInspector({ artifactId }: { artifactId: string }) {
  const { data: artifact, isLoading } = useArtifact(artifactId);
  const { data: versions } = useArtifactVersions(artifactId);

  if (isLoading || !artifact) {
    return (
      <div className="space-y-2 p-4">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-24 w-full" />
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      <div className="border-b border-border p-4">
        <div className="mb-1 text-[10px] uppercase tracking-wide text-muted-foreground">Artifact</div>
        <div className="mb-2 text-sm font-semibold">{artifact.name}</div>
        <div className="flex items-center gap-1.5">
          <StatusBadge status={artifact.status} />
          <Badge variant="outline" className="text-[10px]">
            {artifact.type}
          </Badge>
          <Badge variant="outline" className="text-[10px]">
            v{artifact.version}
          </Badge>
        </div>
        <div className="mt-3 space-y-1 text-xs">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Owner</span>
            <span className="font-medium">{artifact.ownerAgent}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Created</span>
            <span className="font-medium">{new Date(artifact.createdAt).toLocaleString()}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Versions</span>
            <span className="font-medium">{versions?.length ?? 1}</span>
          </div>
        </div>
      </div>

      <div className="p-4">
        <div className="mb-2 text-[10px] uppercase tracking-wide text-muted-foreground">Content</div>
        <pre className="max-h-96 overflow-auto rounded-md bg-secondary/40 p-3 text-[11px] leading-5 whitespace-pre-wrap break-words">
          {artifact.content ?? "(no inline content)"}
        </pre>
      </div>
    </div>
  );
}
