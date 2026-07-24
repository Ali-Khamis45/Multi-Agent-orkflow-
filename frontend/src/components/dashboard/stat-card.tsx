import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

export function StatCard({
  label,
  value,
  icon: Icon,
  tone = "default",
  hint,
}: {
  label: string;
  value: React.ReactNode;
  icon: LucideIcon;
  tone?: "default" | "success" | "warning" | "error" | "running";
  hint?: string;
}) {
  const toneColor = {
    default: "text-foreground",
    success: "text-status-success",
    warning: "text-status-warning",
    error: "text-status-error",
    running: "text-status-running",
  }[tone];

  return (
    <Card className="border-border/60 bg-card/60 py-4 gap-2">
      <CardContent className="px-4">
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">{label}</span>
          <Icon className={cn("h-3.5 w-3.5", toneColor)} />
        </div>
        <div className={cn("mt-1.5 text-2xl font-semibold tabular-nums", toneColor)}>{value}</div>
        {hint && <div className="mt-0.5 text-[11px] text-muted-foreground">{hint}</div>}
      </CardContent>
    </Card>
  );
}
