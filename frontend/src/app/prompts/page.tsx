import { PromptRegistry } from "@/components/prompts/prompt-registry";

export default function PromptsPage() {
  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Prompt Registry</h1>
        <p className="text-xs text-muted-foreground">Every versioned prompt template, its variables, and which agent it powers.</p>
      </div>
      <PromptRegistry />
    </div>
  );
}
