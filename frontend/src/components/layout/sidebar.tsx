"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  GitBranch,
  Bot,
  FileStack,
  BrainCircuit,
  Activity,
  ScrollText,
  Settings,
  Radar,
  HeartPulse,
} from "lucide-react";
import { cn } from "@/lib/utils";

const NAV_ITEMS = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/workflows", label: "Workflow Runs", icon: GitBranch },
  { href: "/agents", label: "Agents", icon: Bot },
  { href: "/artifacts", label: "Artifacts", icon: FileStack },
  { href: "/memory", label: "Memory", icon: BrainCircuit },
  { href: "/telemetry", label: "Telemetry", icon: Activity },
  { href: "/supervisor", label: "Supervisor", icon: Radar },
  { href: "/prompts", label: "Prompt Registry", icon: ScrollText },
  { href: "/health", label: "Project Health", icon: HeartPulse },
  { href: "/settings", label: "Settings", icon: Settings },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden md:flex w-56 shrink-0 flex-col border-r border-sidebar-border bg-sidebar text-sidebar-foreground">
      <div className="flex h-14 items-center gap-2 px-4 border-b border-sidebar-border">
        <div className="flex h-6 w-6 items-center justify-center rounded-md bg-status-running/20 text-status-running">
          <span className="text-xs font-bold">AI</span>
        </div>
        <span className="text-sm font-semibold tracking-tight">Mission Control</span>
      </div>

      <nav className="flex-1 space-y-0.5 p-2">
        {NAV_ITEMS.map((item) => {
          const isActive = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-2.5 rounded-md px-2.5 py-1.5 text-sm transition-colors",
                isActive
                  ? "bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                  : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground",
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-sidebar-border p-3 text-[11px] text-muted-foreground">
        Phase 1.6 · Mission Control
      </div>
    </aside>
  );
}
