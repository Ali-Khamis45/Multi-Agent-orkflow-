"use client";

import { useRouter } from "next/navigation";
import { LogOut } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useAuthStore } from "@/store/auth-store";

export default function FounderSettingsPage() {
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const clearSession = useAuthStore((s) => s.clearSession);

  return (
    <div className="mx-auto max-w-xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Settings</h1>
        <p className="text-xs text-muted-foreground">Your account and workspace.</p>
      </div>

      <Card className="border-border/60">
        <CardHeader>
          <CardTitle className="text-sm">Account</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <div className="flex items-center justify-between border-b border-border/40 py-2">
            <span className="text-xs text-muted-foreground">Name</span>
            <span>{user?.name}</span>
          </div>
          <div className="flex items-center justify-between border-b border-border/40 py-2">
            <span className="text-xs text-muted-foreground">Email</span>
            <span>{user?.email}</span>
          </div>
          <div className="flex items-center justify-between py-2">
            <span className="text-xs text-muted-foreground">Workspace</span>
            <span>Founder</span>
          </div>
        </CardContent>
      </Card>

      <Button
        variant="destructive"
        onClick={() => {
          clearSession();
          router.replace("/login");
        }}
      >
        <LogOut className="h-3.5 w-3.5" /> Log out
      </Button>
    </div>
  );
}
