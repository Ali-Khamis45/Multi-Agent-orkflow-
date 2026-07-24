"use client";

import { Search, Terminal } from "lucide-react";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { Button } from "@/components/ui/button";
import { useConsoleStore } from "@/store/console-store";
import { useCommandPaletteStore } from "@/store/command-palette-store";

export function TopBar() {
  const toggleConsole = useConsoleStore((s) => s.toggle);
  const openPalette = useCommandPaletteStore((s) => s.open);

  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-3 border-b border-border px-4">
      <div className="flex items-center gap-3">
        <WorkspaceSwitcher />
      </div>

      <div className="flex-1 max-w-md">
        <button
          onClick={openPalette}
          className="flex w-full items-center gap-2 rounded-md border border-border bg-secondary/30 px-3 py-1.5 text-xs text-muted-foreground transition-colors hover:bg-secondary/60"
        >
          <Search className="h-3.5 w-3.5" />
          <span className="flex-1 text-left">Search agents, workflows, artifacts…</span>
          <kbd className="rounded border border-border bg-background px-1.5 py-0.5 font-mono text-[10px]">
            ⌘K
          </kbd>
        </button>
      </div>

      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" className="h-8 gap-1.5 text-xs text-muted-foreground" onClick={toggleConsole}>
          <Terminal className="h-3.5 w-3.5" />
          Console
        </Button>
      </div>
    </header>
  );
}
