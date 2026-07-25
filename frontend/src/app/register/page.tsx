"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Code2, Rocket, Check, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card } from "@/components/ui/card";
import { api, ApiError } from "@/lib/api-client";
import { useAuthStore, homeRouteFor, type CompanyType } from "@/store/auth-store";
import { cn } from "@/lib/utils";

const WORKSPACE_OPTIONS: { value: CompanyType; title: string; description: string; icon: typeof Code2 }[] = [
  {
    value: "SoftwareCompany",
    title: "Software Company",
    description: "An autonomous AI engineering team — turns a request into shipped, reviewed, tested code.",
    icon: Code2,
  },
  {
    value: "Founder",
    title: "Founder Workspace",
    description: "A business operating system for founders — turns an idea into a business plan, brand, and launch roadmap.",
    icon: Rocket,
  },
];

export default function RegisterPage() {
  const router = useRouter();
  const setSession = useAuthStore((s) => s.setSession);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [companyType, setCompanyType] = useState<CompanyType | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!companyType) {
      setError("Choose your first Workspace to continue.");
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const result = await api.auth.register(email, password, name, companyType);
      setSession(result.token, {
        userId: result.userId,
        email: result.email,
        name: result.name,
        companyType: result.companyType as CompanyType,
      });
      toast.success(`Welcome, ${result.name}.`);
      router.replace(homeRouteFor(result.companyType as CompanyType));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Registration failed. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-background px-4 py-10">
      <div className="w-full max-w-xl space-y-6">
        <div className="text-center">
          <div className="mx-auto mb-3 flex h-9 w-9 items-center justify-center rounded-md bg-status-running/20 text-status-running">
            <span className="text-sm font-bold">AI</span>
          </div>
          <h1 className="text-lg font-semibold tracking-tight">Welcome</h1>
          <p className="text-xs text-muted-foreground">Create your account and choose your first Workspace.</p>
        </div>

        <Card className="p-6">
          <form onSubmit={handleSubmit} className="space-y-5">
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="name">Name</Label>
                <Input id="name" value={name} onChange={(e) => setName(e.target.value)} required autoComplete="name" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                minLength={8}
                autoComplete="new-password"
              />
              <p className="text-[11px] text-muted-foreground">At least 8 characters.</p>
            </div>

            <div className="space-y-2">
              <Label>Choose your first Workspace</Label>
              <div className="grid gap-3 sm:grid-cols-2">
                {WORKSPACE_OPTIONS.map((opt) => {
                  const Icon = opt.icon;
                  const selected = companyType === opt.value;
                  return (
                    <button
                      type="button"
                      key={opt.value}
                      onClick={() => setCompanyType(opt.value)}
                      className={cn(
                        "group relative flex flex-col gap-2 rounded-xl border p-4 text-left transition-all hover:-translate-y-0.5 hover:shadow-md",
                        selected ? "border-primary bg-primary/5 ring-1 ring-primary" : "border-border/60 bg-card/40 hover:border-border",
                      )}
                    >
                      {selected && (
                        <span className="absolute right-3 top-3 flex h-4 w-4 items-center justify-center rounded-full bg-primary text-primary-foreground">
                          <Check className="h-2.5 w-2.5" />
                        </span>
                      )}
                      <Icon className={cn("h-5 w-5", selected ? "text-primary" : "text-muted-foreground")} />
                      <div className="text-sm font-semibold">{opt.title}</div>
                      <p className="text-xs text-muted-foreground">{opt.description}</p>
                    </button>
                  );
                })}
              </div>
            </div>

            {error && <p className="text-xs text-destructive">{error}</p>}

            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
              Complete Registration
            </Button>
          </form>
        </Card>

        <p className="text-center text-xs text-muted-foreground">
          Already have an account?{" "}
          <Link href="/login" className="font-medium text-foreground underline underline-offset-4">
            Log in
          </Link>
        </p>
      </div>
    </div>
  );
}
