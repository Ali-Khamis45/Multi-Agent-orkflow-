"use client";

import { WorkspaceSwitcher } from "./workspace-switcher";
import { FounderMobileNav } from "./founder-mobile-nav";
import { UserMenu } from "./user-menu";

export function FounderTopBar() {
  return (
    <header className="flex h-14 shrink-0 items-center justify-between gap-2 border-b border-border px-3 sm:gap-3 sm:px-4">
      <div className="flex shrink-0 items-center gap-2">
        <FounderMobileNav />
        <WorkspaceSwitcher />
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <UserMenu />
      </div>
    </header>
  );
}
