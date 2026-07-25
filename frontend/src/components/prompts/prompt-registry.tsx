"use client";

import { useState } from "react";
import { ScrollText, ChevronDown, Sparkles } from "lucide-react";
import { motion, AnimatePresence } from "motion/react";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { usePrompts } from "@/hooks/queries";
import { cn } from "@/lib/utils";

export function PromptRegistry() {
  const { data: prompts, isLoading } = usePrompts();
  const [expanded, setExpanded] = useState<string | null>(null);

  if (isLoading) {
    return <div className="space-y-2">{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}</div>;
  }

  if (!prompts || prompts.length === 0) {
    return <p className="py-16 text-center text-sm text-muted-foreground">No prompts registered yet.</p>;
  }

  return (
    <div className="space-y-2">
      {prompts.map((p) => {
        const isOpen = expanded === p.name;
        const current = p.versions.find((v) => v.version === p.currentVersion) ?? p.versions[p.versions.length - 1];
        return (
          <div key={p.name} className="overflow-hidden rounded-lg border border-border/60 bg-card/60">
            <button
              onClick={() => setExpanded(isOpen ? null : p.name)}
              className="flex w-full items-center gap-3 px-4 py-3 text-left transition-colors hover:bg-secondary/30"
            >
              <ScrollText className="h-4 w-4 shrink-0 text-muted-foreground" />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="truncate text-sm font-semibold">{p.name}</span>
                  <Badge variant="outline" className="text-[10px]">v{p.currentVersion}</Badge>
                </div>
                <div className="mt-0.5 flex items-center gap-2 text-[11px] text-muted-foreground">
                  <span>owner: {p.owner}</span>
                  <span>·</span>
                  <span>compatible: {p.compatibleAgent}</span>
                  <span>·</span>
                  <span>{p.versions.length} version{p.versions.length === 1 ? "" : "s"}</span>
                </div>
              </div>
              <ChevronDown className={cn("h-4 w-4 shrink-0 text-muted-foreground transition-transform", isOpen && "rotate-180")} />
            </button>

            <AnimatePresence initial={false}>
              {isOpen && (
                <motion.div
                  initial={{ height: 0, opacity: 0 }}
                  animate={{ height: "auto", opacity: 1 }}
                  exit={{ height: 0, opacity: 0 }}
                  transition={{ duration: 0.15 }}
                  className="border-t border-border/60"
                >
                  <div className="space-y-3 p-4">
                    {current && (
                      <div>
                        <div className="mb-1 flex items-center gap-1.5 text-[10px] uppercase tracking-wide text-muted-foreground">
                          <Sparkles className="h-3 w-3" /> Current version
                        </div>
                        <p className="text-xs text-muted-foreground">{current.description}</p>
                        <div className="mt-1.5 flex flex-wrap gap-1">
                          {current.variables.map((v) => (
                            <code key={v} className="rounded bg-secondary/60 px-1.5 py-0.5 text-[10px]">{`{{${v}}}`}</code>
                          ))}
                        </div>
                        <p className="mt-1.5 font-mono text-[10px] text-muted-foreground/70">{current.file}</p>
                      </div>
                    )}

                    <div>
                      <div className="mb-1.5 text-[10px] uppercase tracking-wide text-muted-foreground">Version history</div>
                      <div className="space-y-1">
                        {[...p.versions].sort((a, b) => b.version - a.version).map((v) => (
                          <div key={v.version} className="flex items-center gap-2 rounded-md bg-secondary/30 px-2.5 py-1.5 text-[11px]">
                            <Badge variant="outline" className="text-[10px]">v{v.version}</Badge>
                            <span className="flex-1 truncate text-muted-foreground">{v.description}</span>
                            <span className="shrink-0 text-[10px] text-muted-foreground/70">{new Date(v.createdAt).toLocaleDateString()}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        );
      })}

      <div className="rounded-lg border border-dashed border-border/60 p-4 text-xs text-muted-foreground">
        Future: automated prompt optimization (A/B scoring across model runs, confidence-weighted version promotion) — not yet built.
      </div>
    </div>
  );
}
