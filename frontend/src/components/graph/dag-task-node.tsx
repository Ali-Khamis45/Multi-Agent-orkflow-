"use client";

import { Handle, Position, type NodeProps } from "@xyflow/react";
import { motion } from "motion/react";
import { RotateCw, Bot } from "lucide-react";
import { cn } from "@/lib/utils";
import type { TaskNode } from "@/lib/types";

const STATUS_RING: Record<string, string> = {
  Pending: "border-status-pending/40",
  Ready: "border-status-running/50",
  Dispatched: "border-status-running ring-2 ring-status-running/30",
  Running: "border-status-running ring-2 ring-status-running/30",
  Completed: "border-status-success/60",
  Failed: "border-status-error ring-2 ring-status-error/30",
  Blocked: "border-status-error/50",
  WaitingApproval: "border-status-warning/60",
};

const STATUS_DOT: Record<string, string> = {
  Pending: "bg-status-pending",
  Ready: "bg-status-running",
  Dispatched: "bg-status-running",
  Running: "bg-status-running",
  Completed: "bg-status-success",
  Failed: "bg-status-error",
  Blocked: "bg-status-error",
  WaitingApproval: "bg-status-warning",
};

export type DagTaskNodeData = { task: TaskNode };

export function DagTaskNode({ data, selected }: NodeProps & { data: DagTaskNodeData }) {
  const { task } = data;
  const isLive = task.status === "Running" || task.status === "Dispatched";

  return (
    <motion.div
      layout
      initial={{ opacity: 0, scale: 0.9 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.2 }}
      className={cn(
        "w-56 rounded-lg border-2 bg-card px-3 py-2.5 shadow-sm transition-colors",
        STATUS_RING[task.status] ?? "border-border",
        selected && "outline outline-2 outline-offset-2 outline-status-running",
      )}
    >
      <Handle type="target" position={Position.Left} className="!bg-border" />
      <Handle type="source" position={Position.Right} className="!bg-border" />

      <div className="flex items-center justify-between gap-2">
        <span className="truncate text-xs font-semibold">{task.name}</span>
        <span className={cn("h-2 w-2 shrink-0 rounded-full", STATUS_DOT[task.status], isLive && "animate-pulse")} />
      </div>

      <div className="mt-1 flex items-center gap-1 text-[10px] text-muted-foreground">
        <Bot className="h-3 w-3" />
        <span className="truncate">{task.assignedAgentName ?? "unassigned"}</span>
      </div>

      <div className="mt-2 flex items-center justify-between text-[10px]">
        <span className="text-muted-foreground">{task.status}</span>
        <div className="flex items-center gap-1.5">
          {task.attemptCount > 1 && (
            <span className="flex items-center gap-0.5 text-status-warning">
              <RotateCw className="h-2.5 w-2.5" />
              {task.attemptCount}
            </span>
          )}
          {task.confidence !== null && (
            <span className="tabular-nums font-medium">{(task.confidence * 100).toFixed(0)}%</span>
          )}
        </div>
      </div>
    </motion.div>
  );
}
