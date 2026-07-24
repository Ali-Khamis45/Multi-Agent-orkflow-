import { IntakeForm } from "@/components/workflows/intake-form";
import { RunsTable } from "@/components/workflows/runs-table";

export default function WorkflowsPage() {
  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Workflow Runs</h1>
        <p className="text-xs text-muted-foreground">
          Submit a request and watch Intent Analysis, Supervisor decisions, and the dynamic DAG execute live.
        </p>
      </div>

      <IntakeForm />

      <RunsTable />
    </div>
  );
}
