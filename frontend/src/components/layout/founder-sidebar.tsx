"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  FileText,
  Lightbulb,
  Search,
  Swords,
  Palette,
  Package,
  Tag,
  Megaphone,
  Landmark,
  Cog,
  TrendingUp,
  Rocket,
  FolderOpen,
  Settings,
} from "lucide-react";
import { cn } from "@/lib/utils";

export const FOUNDER_NAV_ITEMS = [
  { href: "/founder", label: "Dashboard", icon: LayoutDashboard },
  { href: "/founder/business-plan", label: "Business Plan", icon: FileText },
  { href: "/founder/idea-validation", label: "Idea Validation", icon: Lightbulb },
  { href: "/founder/market-research", label: "Market Research", icon: Search },
  { href: "/founder/competitor-analysis", label: "Competitor Analysis", icon: Swords },
  { href: "/founder/brand-identity", label: "Brand Identity", icon: Palette },
  { href: "/founder/products", label: "Products", icon: Package },
  { href: "/founder/pricing", label: "Pricing", icon: Tag },
  { href: "/founder/marketing", label: "Marketing", icon: Megaphone },
  { href: "/founder/finance", label: "Finance", icon: Landmark },
  { href: "/founder/operations", label: "Operations", icon: Cog },
  { href: "/founder/growth", label: "Growth", icon: TrendingUp },
  { href: "/founder/launch-roadmap", label: "Launch Roadmap", icon: Rocket },
  { href: "/founder/documents", label: "Documents", icon: FolderOpen },
  { href: "/founder/settings", label: "Settings", icon: Settings },
];

export function FounderSidebarBrand() {
  return (
    <div className="flex h-14 items-center gap-2 border-b border-sidebar-border px-4">
      <div className="flex h-6 w-6 items-center justify-center rounded-md bg-amber-500/20 text-amber-500">
        <Rocket className="h-3.5 w-3.5" />
      </div>
      <span className="text-sm font-semibold tracking-tight">Founder Workspace</span>
    </div>
  );
}

export function FounderSidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();

  return (
    <nav className="flex-1 space-y-0.5 overflow-y-auto p-2" aria-label="Primary">
      {FOUNDER_NAV_ITEMS.map((item) => {
        const isActive = item.href === "/founder" ? pathname === "/founder" : pathname.startsWith(item.href);
        const Icon = item.icon;
        return (
          <Link
            key={item.href}
            href={item.href}
            onClick={onNavigate}
            aria-current={isActive ? "page" : undefined}
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
  );
}

export function FounderSidebar() {
  return (
    <aside className="hidden md:flex w-56 shrink-0 flex-col border-r border-sidebar-border bg-sidebar text-sidebar-foreground">
      <FounderSidebarBrand />
      <FounderSidebarNav />
      <div className="border-t border-sidebar-border p-3 text-[11px] text-muted-foreground">
        AI Enterprise OS · Founder
      </div>
    </aside>
  );
}
