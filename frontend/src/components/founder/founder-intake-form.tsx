"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Sparkles, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useSubmitIntake } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";

const EXAMPLES = [
  "A subscription box for artisanal coffee beans",
  "A mobile app that helps freelancers track invoices",
  "A local marketplace connecting home bakers with buyers",
];

export function FounderIntakeForm() {
  const [rawInput, setRawInput] = useState("");
  const router = useRouter();
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const submit = useSubmitIntake();

  const handleSubmit = () => {
    if (!rawInput.trim()) return;
    submit.mutate(
      { rawInput, workspaceId: workspaceId ?? undefined },
      {
        onSuccess: (result) => {
          toast.success("Venture kicked off", { description: "CEO → Business Model → Market & Customer Research…" });
          router.push(`/founder/workflows/${result.workflowRunId}`);
        },
        onError: (err) => {
          toast.error("Failed to start", { description: String(err) });
        },
      },
    );
  };

  return (
    <Card className="border-border/60 bg-card/60">
      <CardContent className="space-y-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          <Sparkles className="h-4 w-4 text-amber-500" />
          Describe your business idea
        </div>
        <Textarea
          value={rawInput}
          onChange={(e) => setRawInput(e.target.value)}
          placeholder="A subscription box for artisanal coffee beans"
          className="min-h-20 resize-none bg-secondary/30"
          onKeyDown={(e) => {
            if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) handleSubmit();
          }}
        />
        <div className="flex items-center justify-between">
          <div className="flex flex-wrap gap-1.5">
            {EXAMPLES.map((ex) => (
              <button
                key={ex}
                onClick={() => setRawInput(ex)}
                className="rounded-full border border-border/60 px-2.5 py-1 text-[11px] text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-foreground"
              >
                {ex}
              </button>
            ))}
          </div>
          <Button onClick={handleSubmit} disabled={submit.isPending || !rawInput.trim()} size="sm">
            {submit.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Sparkles className="h-3.5 w-3.5" />}
            Run
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
