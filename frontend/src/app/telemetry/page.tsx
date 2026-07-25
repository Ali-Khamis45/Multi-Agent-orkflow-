import { TelemetryCenter } from "@/components/telemetry/telemetry-center";

export default function TelemetryPage() {
  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Telemetry Center</h1>
        <p className="text-xs text-muted-foreground">Full observability across every agent, reasoning stage, and workflow run.</p>
      </div>
      <TelemetryCenter />
    </div>
  );
}
