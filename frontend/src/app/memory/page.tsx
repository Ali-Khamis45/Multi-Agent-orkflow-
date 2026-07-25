import { MemoryInspector } from "@/components/memory/memory-inspector";

export default function MemoryPage() {
  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Memory</h1>
        <p className="text-xs text-muted-foreground">The five-layer memory model — what every agent has read and written.</p>
      </div>
      <MemoryInspector />
    </div>
  );
}
