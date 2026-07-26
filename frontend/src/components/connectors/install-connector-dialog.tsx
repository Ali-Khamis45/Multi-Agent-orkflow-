"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useInstallConnector } from "@/hooks/queries";
import type { ConnectorCatalogEntry } from "@/lib/api-client";

const FIELD_LABELS: Record<string, string> = {
  storeUrl: "Store URL (e.g. your-store.myshopify.com)",
  accessToken: "Access token",
  consumerKey: "Consumer key",
  consumerSecret: "Consumer secret",
  secretKey: "Secret key",
  apiKey: "API key",
  parentPageId: "Parent page ID",
  propertyId: "GA4 property ID",
  customerId: "Customer ID",
  developerToken: "Developer token",
  username: "Username",
  botToken: "Bot token",
  organization: "Organization",
  personalAccessToken: "Personal access token",
  cloudId: "Cloud ID",
};

export function InstallConnectorDialog({
  connector,
  workspaceId,
  open,
  onOpenChange,
}: {
  connector: ConnectorCatalogEntry | null;
  workspaceId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const [values, setValues] = useState<Record<string, string>>({});
  const install = useInstallConnector();

  if (!connector) return null;

  const canSubmit = connector.requiredCredentialFields.every((f) => (values[f] ?? "").trim().length > 0);

  async function handleSubmit() {
    if (!connector) return;
    try {
      await install.mutateAsync({ key: connector.key, workspaceId, credentials: values });
      toast.success(`${connector.displayName} connected.`);
      setValues({});
      onOpenChange(false);
    } catch (err) {
      toast.error(`Could not connect ${connector.displayName}`, { description: String(err) });
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Connect {connector.displayName}</DialogTitle>
          <DialogDescription>{connector.description}</DialogDescription>
        </DialogHeader>
        <div className="space-y-3">
          {connector.requiredCredentialFields.map((field) => (
            <div key={field} className="space-y-1.5">
              <Label htmlFor={field}>{FIELD_LABELS[field] ?? field}</Label>
              <Input
                id={field}
                type={field.toLowerCase().includes("secret") || field.toLowerCase().includes("token") || field.toLowerCase().includes("key") ? "password" : "text"}
                value={values[field] ?? ""}
                onChange={(e) => setValues((prev) => ({ ...prev, [field]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        <DialogFooter>
          <Button onClick={handleSubmit} disabled={!canSubmit || install.isPending}>
            {install.isPending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
            Connect
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
