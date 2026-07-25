import { ProjectHealth } from "@/components/health/project-health";

export default function HealthPage() {
  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Project Health</h1>
        <p className="text-xs text-muted-foreground">A composite engineering score, computed from real execution data — not simulated.</p>
      </div>
      <ProjectHealth />
    </div>
  );
}
