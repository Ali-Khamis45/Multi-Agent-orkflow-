"use client";

import { useEffect, useRef } from "react";
import { motion, AnimatePresence } from "motion/react";
import { ChevronDown, Trash2, Terminal } from "lucide-react";
import { useConsoleStore } from "@/store/console-store";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const LEVEL_COLOR: Record<string, string> = {
  info: "text-muted-foreground",
  warn: "text-status-warning",
  error: "text-status-error",
};

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString([], { hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit" });
  } catch {
    return iso;
  }
}

export function EventConsole() {
  const { entries, isOpen, toggle, clear } = useConsoleStore();
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [entries.length]);

  return (
    <div
      className={cn(
        "shrink-0 border-t border-border bg-[oklch(0.1_0_0)] transition-[height] duration-200 ease-out",
        isOpen ? "h-52" : "h-9",
      )}
    >
      <div className="flex h-9 items-center justify-between border-b border-border/60 px-3">
        <button onClick={toggle} className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground">
          <Terminal className="h-3.5 w-3.5" />
          <span className="font-medium">Event Console</span>
          <span className="rounded-full bg-secondary px-1.5 py-0.5 text-[10px] tabular-nums">{entries.length}</span>
          <ChevronDown className={cn("h-3.5 w-3.5 transition-transform", isOpen ? "" : "-rotate-90")} />
        </button>
        {isOpen && (
          <Button variant="ghost" size="icon" className="h-6 w-6" onClick={clear} title="Clear" aria-label="Clear event console">
            <Trash2 className="h-3.5 w-3.5" />
          </Button>
        )}
      </div>

      {isOpen && (
        <div ref={scrollRef} className="h-[calc(100%-2.25rem)] overflow-y-auto px-3 py-1.5 font-mono text-[11px] leading-5">
          {entries.length === 0 && (
            <div className="py-6 text-center text-muted-foreground">
              Waiting for live events — join a workflow run to see reasoning stages, tool calls, and supervisor decisions stream in.
            </div>
          )}
          <AnimatePresence initial={false}>
            {entries.map((e) => (
              <motion.div
                key={e.id}
                initial={{ opacity: 0, y: -4 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.15 }}
                className="flex items-start gap-2"
              >
                <span className="shrink-0 text-muted-foreground/60">{formatTime(e.timestamp)}</span>
                <span className={cn("shrink-0 font-semibold", LEVEL_COLOR[e.level])}>{e.type}</span>
                <span className="text-muted-foreground/80">by {e.producedBy}</span>
                {e.confidence !== null && (
                  <span className="text-muted-foreground/60">conf={e.confidence.toFixed(2)}</span>
                )}
                {e.riskLevel && <span className="text-muted-foreground/60">risk={e.riskLevel}</span>}
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}
    </div>
  );
}
