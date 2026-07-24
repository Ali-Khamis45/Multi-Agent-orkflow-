"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/shared/status-badge";
import { useAgents } from "@/hooks/queries";
import { useInspectorStore } from "@/store/inspector-store";

export function AgentFleet() {
  const { data: agents, isLoading } = useAgents();
  const openInspector = useInspectorStore((s) => s.open);

  return (
    <Card className="border-border/60">
      <CardHeader>
        <CardTitle className="text-sm">Agent Fleet</CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading && (
          <div className="grid grid-cols-2 gap-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-16 w-full" />
            ))}
          </div>
        )}

        <div className="grid grid-cols-2 gap-2">
          {agents?.map((agent) => (
            <button
              key={agent.name}
              onClick={() => openInspector({ kind: "agent", agentName: agent.name })}
              className="rounded-lg border border-border/60 p-2.5 text-left transition-colors hover:bg-secondary/40"
            >
              <div className="flex items-center justify-between gap-1">
                <span className="truncate text-xs font-medium">{agent.name}</span>
                <StatusBadge status={agent.status} className="shrink-0" />
              </div>
              <div className="mt-1 text-[11px] text-muted-foreground">
                {agent.inFlightTaskCount} in-flight · priority {agent.priority}
              </div>
            </button>
          ))}
        </div>

        {!isLoading && (!agents || agents.length === 0) && (
          <p className="py-6 text-center text-xs text-muted-foreground">No agents registered yet.</p>
        )}
      </CardContent>
    </Card>
  );
}
