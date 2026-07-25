"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export function ChartCard({ title, subtitle, children, className }: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <Card className={`border-border/60 ${className ?? ""}`}>
      <CardHeader className="pb-1">
        <CardTitle className="text-sm">{title}</CardTitle>
        {subtitle && <p className="text-[11px] text-muted-foreground">{subtitle}</p>}
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

export const CHART_GRID = "var(--color-border)";
export const CHART_MUTED = "var(--color-muted-foreground)";
export const CHART_ACCENT = "var(--color-status-running)";
export const CHART_SUCCESS = "var(--color-status-success)";
export const CHART_ERROR = "var(--color-status-error)";
export const CHART_WARNING = "var(--color-status-warning)";

export const TOOLTIP_STYLE = {
  contentStyle: {
    background: "var(--color-popover)",
    border: "1px solid var(--color-border)",
    borderRadius: 8,
    fontSize: 11,
    color: "var(--color-popover-foreground)",
  },
  labelStyle: { color: "var(--color-muted-foreground)" },
  cursor: { fill: "var(--color-secondary)", opacity: 0.4 },
};
