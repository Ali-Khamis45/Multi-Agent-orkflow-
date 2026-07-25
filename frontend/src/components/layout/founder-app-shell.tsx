"use client";

import { FounderSidebar } from "./founder-sidebar";
import { FounderTopBar } from "./founder-top-bar";

export function FounderAppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex h-screen w-full overflow-hidden">
      <FounderSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <FounderTopBar />
        <main className="min-h-0 flex-1 overflow-y-auto">{children}</main>
      </div>
    </div>
  );
}
