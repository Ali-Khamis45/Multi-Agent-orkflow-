"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, ArrowRight, Loader2, Rocket } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent } from "@/components/ui/card";
import { useCompleteOnboarding } from "@/hooks/queries";
import { useEnsureWorkspaceSelected } from "@/hooks/use-ensure-workspace";
import type { CompanyProfileData } from "@/lib/api-client";
import { cn } from "@/lib/utils";

const BUILDING_EXAMPLES = ["Fashion Brand", "Coffee Shop", "Restaurant", "Gym", "SaaS", "Agency", "Cosmetics", "Other"];

interface Answers {
  building: string;
  companyName: string;
  location: string;
  problem: string;
  customers: string;
  products: string;
  budget: string;
  launchDate: string;
  extra: string;
}

const EMPTY_ANSWERS: Answers = {
  building: "", companyName: "", location: "", problem: "", customers: "", products: "", budget: "", launchDate: "", extra: "",
};

function buildProfilePatch(a: Answers): Partial<CompanyProfileData> {
  return {
    basicInfo: {
      companyName: a.companyName.trim() || null,
      industry: a.building.trim() || null,
      businessType: a.building.trim() || null,
      country: a.location.trim() || null,
      city: null,
      launchStage: "Idea",
      businessDescription: a.problem.trim() || null,
      notes: a.extra.trim() || null,
    },
    customers: {
      targetAudience: a.customers.trim() || null,
      personas: [],
      problems: [],
      goals: [],
      notes: null,
    },
    products: {
      catalog: [],
      categories: a.products.trim() ? a.products.split(",").map((s) => s.trim()).filter(Boolean) : [],
      manufacturingStrategy: null,
      pricingStrategy: null,
      notes: null,
    },
    business: {
      revenueModel: null,
      budget: a.budget.trim() ? Number(a.budget) : null,
      fundingStatus: null,
      monthlyRevenueGoal: null,
      growthGoal: null,
      launchDate: a.launchDate.trim() || null,
      notes: null,
    },
  };
}

export default function FounderOnboardingPage() {
  const router = useRouter();
  const workspaceId = useEnsureWorkspaceSelected();
  const completeOnboarding = useCompleteOnboarding();
  const [step, setStep] = useState(0);
  const [answers, setAnswers] = useState<Answers>(EMPTY_ANSWERS);

  const steps: {
    question: string;
    field: keyof Answers;
    placeholder: string;
    type?: "text" | "number" | "date" | "textarea";
    examples?: string[];
    optional?: boolean;
  }[] = [
    { question: "What are you building?", field: "building", placeholder: "e.g. Fashion Brand", examples: BUILDING_EXAMPLES },
    { question: "What is your business name?", field: "companyName", placeholder: "e.g. Steepwell Tea Co." },
    { question: "Where will you operate?", field: "location", placeholder: "e.g. United States" },
    { question: "What problem are you solving?", field: "problem", placeholder: "Describe the problem your business solves…", type: "textarea" },
    { question: "Who are your customers?", field: "customers", placeholder: "Describe your target audience…", type: "textarea" },
    { question: "What products will you sell?", field: "products", placeholder: "e.g. Hoodies, T-shirts, Accessories (comma-separated)" },
    { question: "How much budget do you have?", field: "budget", placeholder: "e.g. 5000", type: "number", optional: true },
    { question: "When do you want to launch?", field: "launchDate", placeholder: "", type: "date", optional: true },
    { question: "Anything else I should know?", field: "extra", placeholder: "Optional — anything that helps your AI team understand the business…", type: "textarea", optional: true },
  ];

  const current = steps[step];
  const isLastStep = step === steps.length - 1;
  const canProceed = current.optional || answers[current.field].trim().length > 0;

  function update(value: string) {
    setAnswers((prev) => ({ ...prev, [current.field]: value }));
  }

  async function handleNext() {
    if (!isLastStep) {
      setStep((s) => s + 1);
      return;
    }
    if (!workspaceId) return;
    try {
      await completeOnboarding.mutateAsync({ workspaceId, profile: buildProfilePatch(answers) });
      toast.success("Your Company Profile is ready.");
      router.replace("/founder");
    } catch (err) {
      toast.error("Could not save your Company Profile", { description: String(err) });
    }
  }

  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-background px-4 py-10">
      <div className="w-full max-w-lg space-y-6">
        <div className="text-center">
          <div className="mx-auto mb-3 flex h-9 w-9 items-center justify-center rounded-md bg-amber-500/20 text-amber-500">
            <Rocket className="h-4 w-4" />
          </div>
          <h1 className="text-lg font-semibold tracking-tight">Let&apos;s set up your business</h1>
          <p className="text-xs text-muted-foreground">
            A few quick questions so your AI team never has to ask twice. Step {step + 1} of {steps.length}.
          </p>
        </div>

        <div className="flex gap-1">
          {steps.map((_, i) => (
            <div key={i} className={cn("h-1 flex-1 rounded-full", i <= step ? "bg-amber-500" : "bg-secondary")} />
          ))}
        </div>

        <Card className="p-6">
          <CardContent className="space-y-4 px-0">
            <label className="text-sm font-medium">{current.question}</label>

            {current.examples && (
              <div className="flex flex-wrap gap-1.5">
                {current.examples.map((ex) => (
                  <button
                    key={ex}
                    type="button"
                    onClick={() => update(ex)}
                    className={cn(
                      "rounded-full border px-2.5 py-1 text-[11px] transition-colors",
                      answers[current.field] === ex
                        ? "border-amber-500 bg-amber-500/10 text-amber-600"
                        : "border-border/60 text-muted-foreground hover:bg-secondary/60 hover:text-foreground",
                    )}
                  >
                    {ex}
                  </button>
                ))}
              </div>
            )}

            {current.type === "textarea" ? (
              <Textarea
                autoFocus
                value={answers[current.field]}
                onChange={(e) => update(e.target.value)}
                placeholder={current.placeholder}
                className="min-h-24 resize-none bg-secondary/30"
              />
            ) : (
              <Input
                autoFocus
                type={current.type ?? "text"}
                value={answers[current.field]}
                onChange={(e) => update(e.target.value)}
                placeholder={current.placeholder}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && canProceed) handleNext();
                }}
              />
            )}

            <div className="flex items-center justify-between pt-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={step === 0}
                onClick={() => setStep((s) => Math.max(0, s - 1))}
              >
                <ArrowLeft className="h-3.5 w-3.5" /> Back
              </Button>
              <Button type="button" size="sm" disabled={!canProceed || completeOnboarding.isPending} onClick={handleNext}>
                {completeOnboarding.isPending ? (
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                ) : isLastStep ? (
                  "Complete setup"
                ) : (
                  <>
                    Next <ArrowRight className="h-3.5 w-3.5" />
                  </>
                )}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
