"use client";

import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";
import { InspectorPanel } from "./inspector-panel";
import { EventConsole } from "./event-console";
import { CommandPalette } from "./command-palette";

export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex h-screen w-full overflow-hidden">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <div className="flex min-h-0 flex-1">
          <main className="min-w-0 flex-1 overflow-y-auto">{children}</main>
          <InspectorPanel />
        </div>
        <EventConsole />
      </div>
      <CommandPalette />
    </div>
  );
}
