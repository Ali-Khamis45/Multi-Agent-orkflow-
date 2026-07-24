import { cn } from "@/lib/utils";

const STATUS_STYLES: Record<string, string> = {
  Running: "bg-status-running/15 text-status-running border-status-running/30",
  Dispatched: "bg-status-running/15 text-status-running border-status-running/30",
  Ready: "bg-status-running/10 text-status-running border-status-running/20",
  Completed: "bg-status-success/15 text-status-success border-status-success/30",
  Final: "bg-status-success/15 text-status-success border-status-success/30",
  Available: "bg-status-success/15 text-status-success border-status-success/30",
  Failed: "bg-status-error/15 text-status-error border-status-error/30",
  Unavailable: "bg-status-error/15 text-status-error border-status-error/30",
  Blocked: "bg-status-error/15 text-status-error border-status-error/30",
  WaitingApproval: "bg-status-warning/15 text-status-warning border-status-warning/30",
  Paused: "bg-status-warning/15 text-status-warning border-status-warning/30",
  Busy: "bg-status-warning/15 text-status-warning border-status-warning/30",
  Pending: "bg-status-pending/15 text-muted-foreground border-status-pending/30",
  Planning: "bg-status-pending/15 text-muted-foreground border-status-pending/30",
  Draft: "bg-status-pending/15 text-muted-foreground border-status-pending/30",
  Superseded: "bg-status-pending/10 text-muted-foreground border-status-pending/20",
  RolledBack: "bg-status-pending/10 text-muted-foreground border-status-pending/20",
};

export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const style = STATUS_STYLES[status] ?? "bg-status-pending/15 text-muted-foreground border-status-pending/30";
  const isLive = status === "Running" || status === "Dispatched";

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-[11px] font-medium",
        style,
        className,
      )}
    >
      <span className={cn("h-1.5 w-1.5 rounded-full bg-current", isLive && "animate-pulse")} />
      {status}
    </span>
  );
}
