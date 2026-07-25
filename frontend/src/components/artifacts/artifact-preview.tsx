"use client";

import dynamic from "next/dynamic";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { artifactLanguage } from "@/lib/artifact-lang";
import type { Artifact } from "@/lib/types";

const MonacoEditor = dynamic(() => import("@monaco-editor/react").then((m) => m.default), { ssr: false });

const MARKDOWN_CLASSES =
  "max-w-none text-sm leading-relaxed text-foreground/90 " +
  "[&_h1]:mt-5 [&_h1]:mb-2 [&_h1]:text-lg [&_h1]:font-semibold [&_h1]:first:mt-0 " +
  "[&_h2]:mt-4 [&_h2]:mb-2 [&_h2]:text-base [&_h2]:font-semibold " +
  "[&_h3]:mt-3 [&_h3]:mb-1.5 [&_h3]:text-sm [&_h3]:font-semibold " +
  "[&_p]:my-2 [&_ul]:my-2 [&_ul]:list-disc [&_ul]:pl-5 [&_ol]:my-2 [&_ol]:list-decimal [&_ol]:pl-5 " +
  "[&_li]:my-0.5 [&_a]:text-status-running [&_a]:underline [&_strong]:font-semibold " +
  "[&_code]:rounded [&_code]:bg-secondary/60 [&_code]:px-1 [&_code]:py-0.5 [&_code]:text-[0.85em] " +
  "[&_pre]:my-2 [&_pre]:overflow-x-auto [&_pre]:rounded-md [&_pre]:bg-secondary/40 [&_pre]:p-3 [&_pre_code]:bg-transparent [&_pre_code]:p-0 " +
  "[&_blockquote]:my-2 [&_blockquote]:border-l-2 [&_blockquote]:border-border [&_blockquote]:pl-3 [&_blockquote]:text-muted-foreground " +
  "[&_table]:my-2 [&_table]:w-full [&_table]:border-collapse [&_th]:border [&_th]:border-border [&_th]:px-2 [&_th]:py-1 [&_td]:border [&_td]:border-border [&_td]:px-2 [&_td]:py-1 " +
  "[&_hr]:my-4 [&_hr]:border-border";

export function ArtifactPreview({ artifact }: { artifact: Artifact }) {
  const content = artifact.content ?? "";

  if (!content) {
    return <p className="p-6 text-center text-sm text-muted-foreground">This version has no inline content.</p>;
  }

  if (artifact.type === "Markdown") {
    return (
      <div className={MARKDOWN_CLASSES}>
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
      </div>
    );
  }

  const language = artifactLanguage(artifact.name, artifact.type);
  const formatted = language === "json" ? tryPrettyJson(content) : content;

  return (
    <div className="h-[520px] overflow-hidden rounded-md border border-border/60">
      <MonacoEditor
        language={language}
        value={formatted}
        theme="vs-dark"
        options={{ readOnly: true, minimap: { enabled: false }, fontSize: 12, scrollBeyondLastLine: false, wordWrap: "on" }}
      />
    </div>
  );
}

function tryPrettyJson(content: string): string {
  try {
    return JSON.stringify(JSON.parse(content), null, 2);
  } catch {
    return content;
  }
}
