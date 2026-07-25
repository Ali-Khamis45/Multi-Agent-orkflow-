import { SupervisorBrain } from "@/components/supervisor/supervisor-brain";

export default function SupervisorPage() {
  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Supervisor Brain</h1>
        <p className="text-xs text-muted-foreground">Every strategy, replan, retry, and reassignment decision — across the whole workspace.</p>
      </div>
      <SupervisorBrain />
    </div>
  );
}
