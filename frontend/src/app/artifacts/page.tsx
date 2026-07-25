import { ArtifactsExplorer } from "@/components/artifacts/artifacts-explorer";

export default function ArtifactsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col gap-4 p-6">
      <div className="shrink-0">
        <h1 className="text-lg font-semibold tracking-tight">Artifacts</h1>
        <p className="text-xs text-muted-foreground">Every requirement, design doc, code file, and report the fleet has produced.</p>
      </div>
      <div className="min-h-0 flex-1">
        <ArtifactsExplorer />
      </div>
    </div>
  );
}
