import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** "backend-engineer" -> "Backend Engineer" — agents have no separate role
 * field, so the role shown in the UI is derived directly from the real
 * registered agent name rather than invented. */
export function agentRole(agentName: string): string {
  return agentName
    .split(/[-_]/)
    .filter(Boolean)
    .map((w) => w[0].toUpperCase() + w.slice(1))
    .join(" ");
}

export function initials(name: string): string {
  const parts = name.split(/[-_\s]/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[1][0]).toUpperCase();
}
