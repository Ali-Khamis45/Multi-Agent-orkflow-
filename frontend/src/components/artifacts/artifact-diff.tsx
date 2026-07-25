"use client";

import { useState } from "react";
import dynamic from "next/dynamic";
import { artifactLanguage } from "@/lib/artifact-lang";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { Artifact } from "@/lib/types";

const DiffEditor = dynamic(() => import("@monaco-editor/react").then((m) => m.DiffEditor), { ssr: false });

export function ArtifactDiff({ versions }: { versions: Artifact[] }) {
  // versions: newest first (per GetArtifactVersionsQuery reversed to newest-first below by caller).
  const [fromId, setFromId] = useState<string | null>(versions[1]?.id ?? null);
  const [toId, setToId] = useState<string | null>(versions[0]?.id ?? null);

  if (versions.length < 2) {
    return <p className="p-6 text-center text-sm text-muted-foreground">Only one version exists — nothing to diff yet.</p>;
  }

  const from = versions.find((v) => v.id === fromId) ?? versions[1];
  const to = versions.find((v) => v.id === toId) ?? versions[0];
  const language = artifactLanguage(to.name, to.type);

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-xs">
        <span className="text-muted-foreground">Compare</span>
        <Select value={from.id} onValueChange={(v) => v && setFromId(v)}>
          <SelectTrigger className="h-7 text-xs"><SelectValue placeholder="From" /></SelectTrigger>
          <SelectContent>
            {versions.map((v) => <SelectItem key={v.id} value={v.id}>v{v.version} · {new Date(v.createdAt).toLocaleString()}</SelectItem>)}
          </SelectContent>
        </Select>
        <span className="text-muted-foreground">→</span>
        <Select value={to.id} onValueChange={(v) => v && setToId(v)}>
          <SelectTrigger className="h-7 text-xs"><SelectValue placeholder="To" /></SelectTrigger>
          <SelectContent>
            {versions.map((v) => <SelectItem key={v.id} value={v.id}>v{v.version} · {new Date(v.createdAt).toLocaleString()}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>
      <div className="h-[480px] overflow-hidden rounded-md border border-border/60">
        <DiffEditor
          language={language}
          original={from.content ?? ""}
          modified={to.content ?? ""}
          theme="vs-dark"
          options={{ readOnly: true, minimap: { enabled: false }, fontSize: 12, renderSideBySide: true }}
        />
      </div>
    </div>
  );
}
