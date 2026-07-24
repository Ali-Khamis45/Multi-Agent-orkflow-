// SignalR connection to the ASP.NET API's WorkflowHub (/hubs/workflow) —
// the dashboard's only source of live updates (Phase 1.6: "communicate only
// with the ASP.NET API and SignalR").

import * as signalR from "@microsoft/signalr";
import type { WorkflowEvent } from "./types";

const HUB_URL = process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5080/hubs/workflow";

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

export function getHubConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function ensureHubStarted(): Promise<void> {
  const hub = getHubConnection();
  if (hub.state === signalR.HubConnectionState.Connected) return;
  if (!startPromise) {
    startPromise = hub.start().catch((err) => {
      startPromise = null;
      throw err;
    });
  }
  await startPromise;
}

export async function joinWorkflow(workflowRunId: string): Promise<void> {
  await ensureHubStarted();
  await getHubConnection().invoke("JoinWorkflow", workflowRunId);
}

export async function leaveWorkflow(workflowRunId: string): Promise<void> {
  const hub = getHubConnection();
  if (hub.state !== signalR.HubConnectionState.Connected) return;
  await hub.invoke("LeaveWorkflow", workflowRunId).catch(() => {});
}

export type { WorkflowEvent };
export { signalR };
