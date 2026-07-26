"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { Loader2, Plug, PlugZap, RefreshCw, Unplug, ExternalLink } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useConnectorCatalog, useInstalledConnectors, useDisconnectConnector,
  useConnectorHealth, useConnectorSync, useConnectorAuthorizeUrl,
} from "@/hooks/queries";
import { InstallConnectorDialog } from "./install-connector-dialog";
import type { ConnectorCatalogEntry } from "@/lib/api-client";
import { cn } from "@/lib/utils";

const STATUS_STYLES: Record<string, string> = {
  Connected: "bg-status-success/15 text-status-success border-status-success/30",
  Disconnected: "bg-status-pending/15 text-muted-foreground border-status-pending/30",
  Error: "bg-status-error/15 text-status-error border-status-error/30",
};

export function ConnectorMarketplace({ workspaceId, accentColor = "text-status-running" }: { workspaceId: string; accentColor?: string }) {
  const searchParams = useSearchParams();
  const { data: catalog, isLoading: catalogLoading } = useConnectorCatalog();
  const { data: installed, isLoading: installedLoading } = useInstalledConnectors(workspaceId);
  const disconnect = useDisconnectConnector();
  const health = useConnectorHealth();
  const sync = useConnectorSync();
  const authorizeUrl = useConnectorAuthorizeUrl();
  const [installTarget, setInstallTarget] = useState<ConnectorCatalogEntry | null>(null);

  useEffect(() => {
    const connected = searchParams.get("connected");
    const error = searchParams.get("error");
    if (connected) toast.success(`${connected} connected.`);
    if (error) toast.error(`Could not connect ${error}.`);
  }, [searchParams]);

  const installedKeys = new Set((installed ?? []).map((c) => c.connectorKey));

  return (
    <div className="space-y-4">
      <Tabs defaultValue="installed">
        <TabsList>
          <TabsTrigger value="installed">Installed ({installed?.length ?? 0})</TabsTrigger>
          <TabsTrigger value="browse">Browse</TabsTrigger>
        </TabsList>

        <TabsContent value="installed" className="mt-4 space-y-3">
          {installedLoading && <Skeleton className="h-24 w-full" />}
          {!installedLoading && (!installed || installed.length === 0) && (
            <p className="py-10 text-center text-sm text-muted-foreground">
              No connectors installed yet — browse the catalog to connect one.
            </p>
          )}
          {installed?.map((c) => (
            <Card key={c.connectorKey} className="border-border/60">
              <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-semibold">{c.displayName}</span>
                    <Badge variant="outline" className={cn("text-[10px]", STATUS_STYLES[c.status])}>{c.status}</Badge>
                  </div>
                  <div className="mt-1 space-y-0.5 text-[11px] text-muted-foreground">
                    <div>
                      Health: {c.lastHealthCheckAt ? (c.lastHealthOk ? "OK" : "Failed") + ` — ${c.lastHealthMessage ?? ""}` : "Not checked yet"}
                    </div>
                    <div>
                      Sync: {c.lastSyncedAt ? (c.lastSyncOk ? "OK" : "Failed") + ` — ${c.lastSyncMessage ?? ""}` : "Not synced yet"}
                    </div>
                  </div>
                </div>
                <div className="flex shrink-0 gap-1.5">
                  <Button
                    size="sm" variant="outline"
                    disabled={health.isPending}
                    onClick={() => health.mutate({ key: c.connectorKey, workspaceId }, {
                      onError: (err) => toast.error("Health check failed", { description: String(err) }),
                    })}
                  >
                    {health.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <PlugZap className="h-3.5 w-3.5" />}
                    Check health
                  </Button>
                  <Button
                    size="sm" variant="outline"
                    disabled={sync.isPending}
                    onClick={() => sync.mutate({ key: c.connectorKey, workspaceId }, {
                      onSuccess: (res) => toast(res.success ? "Sync complete" : "Sync failed", { description: res.summary }),
                      onError: (err) => toast.error("Sync failed", { description: String(err) }),
                    })}
                  >
                    {sync.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
                    Sync now
                  </Button>
                  <Button
                    size="sm" variant="destructive"
                    disabled={disconnect.isPending}
                    onClick={() => disconnect.mutate({ key: c.connectorKey, workspaceId }, {
                      onSuccess: () => toast.success(`${c.displayName} disconnected.`),
                      onError: (err) => toast.error("Disconnect failed", { description: String(err) }),
                    })}
                  >
                    <Unplug className="h-3.5 w-3.5" /> Disconnect
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </TabsContent>

        <TabsContent value="browse" className="mt-4">
          {catalogLoading && <Skeleton className="h-40 w-full" />}
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {catalog?.map((c) => {
              const isInstalled = installedKeys.has(c.key);
              return (
                <Card key={c.key} className="flex flex-col border-border/60">
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2 text-sm">
                      <Plug className={cn("h-3.5 w-3.5", accentColor)} />
                      {c.displayName}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="flex flex-1 flex-col justify-between gap-3">
                    <div>
                      <p className="text-xs text-muted-foreground">{c.description}</p>
                      <div className="mt-2 flex flex-wrap gap-1">
                        {c.actions.map((a) => (
                          <Badge key={a.key} variant="outline" className="text-[10px]">{a.displayName}</Badge>
                        ))}
                      </div>
                    </div>
                    {isInstalled ? (
                      <Badge variant="outline" className={cn("w-fit text-[10px]", STATUS_STYLES.Connected)}>Installed</Badge>
                    ) : c.oAuthAvailable ? (
                      <Button
                        size="sm"
                        disabled={authorizeUrl.isPending}
                        onClick={() =>
                          authorizeUrl.mutate({ key: c.key, workspaceId }, {
                            onSuccess: (res) => { window.location.href = res.url; },
                            onError: (err) => toast.error("Could not start connection", { description: String(err) }),
                          })
                        }
                      >
                        {authorizeUrl.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <ExternalLink className="h-3.5 w-3.5" />}
                        Connect
                      </Button>
                    ) : (
                      <Button size="sm" onClick={() => setInstallTarget(c)}>
                        <Plug className="h-3.5 w-3.5" /> Connect
                      </Button>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </TabsContent>
      </Tabs>

      <InstallConnectorDialog
        connector={installTarget}
        workspaceId={workspaceId}
        open={!!installTarget}
        onOpenChange={(open) => !open && setInstallTarget(null)}
      />
    </div>
  );
}
