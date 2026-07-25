"use client";

import { Search, Terminal } from "lucide-react";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { MobileNav } from "./mobile-nav";
import { UserMenu } from "./user-menu";
import { Button } from "@/components/ui/button";
import { useConsoleStore } from "@/store/console-store";
import { useCommandPaletteStore } from "@/store/command-palette-store";

export function TopBar() {
  const toggleConsole = useConsoleStore((s) => s.toggle);
  const openPalette = useCommandPaletteStore((s) => s.open);

  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-2 border-b border-border px-3 sm:gap-3 sm:px-4">
      <div className="flex shrink-0 items-center gap-2">
        <MobileNav />
        <WorkspaceSwitcher />
      </div>

      <div className="max-w-md flex-1">
        <button
          onClick={openPalette}
          aria-label="Open command palette"
          className="flex w-full items-center gap-2 rounded-md border border-border bg-secondary/30 px-2.5 py-1.5 text-xs text-muted-foreground transition-colors hover:bg-secondary/60 sm:px-3"
        >
          <Search className="h-3.5 w-3.5 shrink-0" />
          <span className="hidden flex-1 text-left sm:inline">Search agents, workflows, artifacts…</span>
          <kbd className="ml-auto hidden rounded border border-border bg-background px-1.5 py-0.5 font-mono text-[10px] sm:inline-block">
            ⌘K
          </kbd>
        </button>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        <Button variant="ghost" size="sm" className="h-8 gap-1.5 text-xs text-muted-foreground" onClick={toggleConsole}>
          <Terminal className="h-3.5 w-3.5" />
          <span className="hidden sm:inline">Console</span>
        </Button>
        <UserMenu />
      </div>
    </header>
  );
}
