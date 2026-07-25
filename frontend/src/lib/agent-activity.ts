import type { TaskNode, WorkflowRun } from "./types";

export interface AgentTaskRef {
  run: WorkflowRun;
  node: TaskNode;
}

const LIVE = new Set(["Running", "Dispatched"]);

/** Every task node ever assigned to `agentName`, newest first, across a set of runs. */
export function agentExecutions(runs: WorkflowRun[] | undefined, agentName: string): AgentTaskRef[] {
  if (!runs) return [];
  const refs: AgentTaskRef[] = [];
  for (const run of runs) {
    for (const node of run.nodes) {
      if (node.assignedAgentName === agentName) refs.push({ run, node });
    }
  }
  return refs.sort((a, b) => b.node.updatedAt.localeCompare(a.node.updatedAt));
}

/** The task this agent is live on right now, if any. */
export function agentCurrentTask(runs: WorkflowRun[] | undefined, agentName: string): AgentTaskRef | null {
  const execs = agentExecutions(runs, agentName);
  return execs.find((e) => LIVE.has(e.node.status)) ?? null;
}
