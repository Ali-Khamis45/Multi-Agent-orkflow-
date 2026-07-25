"use client";

import { AnimatePresence, motion } from "motion/react";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useInspectorStore } from "@/store/inspector-store";
import { TaskNodeInspector } from "@/components/graph/task-node-inspector";
import { ArtifactInspector } from "@/components/artifacts/artifact-inspector";
import { AgentInspector } from "@/components/agents/agent-inspector";

export function InspectorPanel() {
  const { content, close } = useInspectorStore();

  return (
    <AnimatePresence>
      {content && (
        <motion.aside
          initial={{ width: 0, opacity: 0 }}
          animate={{ width: 380, opacity: 1 }}
          exit={{ width: 0, opacity: 0 }}
          transition={{ duration: 0.18, ease: "easeOut" }}
          className="shrink-0 overflow-hidden border-l border-border bg-card"
        >
          <div className="flex h-full w-[380px] flex-col">
            <div className="flex h-11 shrink-0 items-center justify-between border-b border-border px-3">
              <span className="text-xs font-medium text-muted-foreground">Inspector</span>
              <Button variant="ghost" size="icon" className="h-6 w-6" onClick={close} aria-label="Close inspector panel">
                <X className="h-3.5 w-3.5" />
              </Button>
            </div>
            <div className="flex-1 overflow-y-auto">
              {content.kind === "task-node" && (
                <TaskNodeInspector workflowRunId={content.workflowRunId} taskNodeId={content.taskNodeId} />
              )}
              {content.kind === "artifact" && <ArtifactInspector artifactId={content.artifactId} />}
              {content.kind === "agent" && <AgentInspector agentName={content.agentName} />}
            </div>
          </div>
        </motion.aside>
      )}
    </AnimatePresence>
  );
}
