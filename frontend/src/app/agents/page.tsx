import { AgentsGrid } from "@/components/agents/agents-grid";

export default function AgentsPage() {
  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Agents</h1>
        <p className="text-xs text-muted-foreground">The registered agent fleet — status, skills, and live performance.</p>
      </div>
      <AgentsGrid />
    </div>
  );
}
