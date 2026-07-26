"use client";

import { Suspense } from "react";
import { ConnectorMarketplace } from "@/components/connectors/connector-marketplace";
import { useWorkspaceStore } from "@/store/workspace-store";

export default function ConnectorsPage() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);

  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Connectors</h1>
        <p className="text-xs text-muted-foreground">
          Connect GitHub, Slack, Vercel, and more so your AI team can act on real repositories and services.
        </p>
      </div>
      {workspaceId && (
        <Suspense>
          <ConnectorMarketplace workspaceId={workspaceId} />
        </Suspense>
      )}
    </div>
  );
}
