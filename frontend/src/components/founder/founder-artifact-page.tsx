import { ArtifactsExplorer } from "@/components/artifacts/artifacts-explorer";

export function FounderArtifactPage({
  title,
  description,
  defaultSearch,
}: {
  title: string;
  description: string;
  defaultSearch?: string;
}) {
  return (
    <div className="flex h-full min-h-0 flex-col gap-4 p-6">
      <div className="shrink-0">
        <h1 className="text-lg font-semibold tracking-tight">{title}</h1>
        <p className="text-xs text-muted-foreground">{description}</p>
      </div>
      <div className="min-h-0 flex-1">
        <ArtifactsExplorer defaultSearch={defaultSearch} />
      </div>
    </div>
  );
}
