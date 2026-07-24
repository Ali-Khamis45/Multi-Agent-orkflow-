import { create } from "zustand";

export interface ConsoleEntry {
  id: string;
  timestamp: string;
  type: string;
  producedBy: string;
  taskId: string | null;
  confidence: number | null;
  riskLevel: string | null;
  level: "info" | "warn" | "error";
}

interface ConsoleState {
  entries: ConsoleEntry[];
  isOpen: boolean;
  toggle: () => void;
  setOpen: (open: boolean) => void;
  push: (entry: ConsoleEntry) => void;
  clear: () => void;
}

const MAX_ENTRIES = 500;

function levelFor(type: string): ConsoleEntry["level"] {
  if (type.includes("Failed")) return "error";
  if (type.includes("Retry") || type.includes("Unavailable") || type.includes("ClarificationRequested")) return "warn";
  return "info";
}

export const useConsoleStore = create<ConsoleState>((set) => ({
  entries: [],
  isOpen: true,
  toggle: () => set((s) => ({ isOpen: !s.isOpen })),
  setOpen: (open) => set({ isOpen: open }),
  push: (entry) =>
    set((s) => ({
      entries: [...s.entries, { ...entry, level: entry.level ?? levelFor(entry.type) }].slice(-MAX_ENTRIES),
    })),
  clear: () => set({ entries: [] }),
}));
