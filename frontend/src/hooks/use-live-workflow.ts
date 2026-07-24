"use client";

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { getHubConnection, joinWorkflow, leaveWorkflow, type WorkflowEvent } from "@/lib/signalr";
import { useConsoleStore } from "@/store/console-store";

/**
 * Joins the SignalR WorkflowHub group for one run (Phase 1.6: dashboard live
 * updates via SignalR only, per ARCHITECTURE.md §23) and:
 *  - pushes every event into the Event Console
 *  - invalidates the TanStack Query caches the event implies changed, so the
 *    DAG/telemetry/artifacts re-fetch and animate in without a page refresh
 */
export function useLiveWorkflow(workflowRunId: string | undefined) {
  const queryClient = useQueryClient();
  const pushConsoleEntry = useConsoleStore((s) => s.push);
  const idCounter = useRef(0);

  useEffect(() => {
    if (!workflowRunId) return;

    let cancelled = false;
    const connection = getHubConnection();

    const handler = (event: WorkflowEvent) => {
      idCounter.current += 1;
      pushConsoleEntry({
        id: `${Date.now()}-${idCounter.current}`,
        timestamp: event.timestamp,
        type: event.type,
        producedBy: event.producedBy,
        taskId: event.taskId,
        confidence: event.confidence,
        riskLevel: event.riskLevel,
        level: "info",
      });

      queryClient.invalidateQueries({ queryKey: ["workflow-run", workflowRunId] });
      queryClient.invalidateQueries({ queryKey: ["workflow-runs"] });
      queryClient.invalidateQueries({ queryKey: ["agents"] });
      queryClient.invalidateQueries({ queryKey: ["agent-metrics"] });

      if (event.type === "TaskCompleted" || event.type === "TaskFailed") {
        if (event.taskId) {
          queryClient.invalidateQueries({ queryKey: ["reasoning-traces", event.taskId] });
        }
        queryClient.invalidateQueries({ queryKey: ["artifacts"] });
      }
      if (event.type.startsWith("Supervisor")) {
        queryClient.invalidateQueries({ queryKey: ["supervisor-decisions", workflowRunId] });
      }
    };

    connection.on("workflowEvent", handler);

    joinWorkflow(workflowRunId).catch((err) => {
      if (!cancelled) console.error("Failed to join SignalR workflow group", err);
    });

    return () => {
      cancelled = true;
      connection.off("workflowEvent", handler);
      leaveWorkflow(workflowRunId).catch(() => {});
    };
  }, [workflowRunId, queryClient, pushConsoleEntry]);
}
