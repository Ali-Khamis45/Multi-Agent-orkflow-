import { create } from "zustand";

export type InspectorContent =
  | { kind: "task-node"; workflowRunId: string; taskNodeId: string }
  | { kind: "artifact"; artifactId: string }
  | { kind: "agent"; agentName: string }
  | null;

interface InspectorState {
  content: InspectorContent;
  open: (content: NonNullable<InspectorContent>) => void;
  close: () => void;
}

export const useInspectorStore = create<InspectorState>((set) => ({
  content: null,
  open: (content) => set({ content }),
  close: () => set({ content: null }),
}));
