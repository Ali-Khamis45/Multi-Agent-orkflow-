"""Renders a CompanyProfile (see api/Domain/Founders/CompanyProfileJson.cs for the
canonical shape) into plain text for a Founder agent's prompt context — Phase 3's
"Company Memory": every Founder agent gets this for free via
ReasoningPipeline._retrieve_context, so no agent ever has to ask the founder something
already on file.
"""

from __future__ import annotations

import json
from typing import Any

_SECTION_LABELS: dict[str, str] = {
    "basicInfo": "Basic Info",
    "brand": "Brand",
    "products": "Products",
    "customers": "Customers",
    "business": "Business",
    "competition": "Competition",
    "marketing": "Marketing",
    "operations": "Operations",
}


def render_company_profile_context(profile: dict[str, Any]) -> str:
    """Empty string if the profile has nothing set yet (e.g. onboarding hasn't run) —
    callers should skip adding an empty/noisy header in that case."""
    blocks: list[str] = []

    for key, label in _SECTION_LABELS.items():
        section = profile.get(key) or {}
        lines: list[str] = []
        for field, value in section.items():
            if field == "notes" or value in (None, "", [], {}):
                continue
            if isinstance(value, list):
                if all(isinstance(v, str) for v in value):
                    value_str = ", ".join(value)
                else:
                    value_str = json.dumps(value)
            else:
                value_str = str(value)
            lines.append(f"- {field}: {value_str}")

        notes = section.get("notes")
        if notes:
            lines.append(f"- notes: {notes}")

        if lines:
            blocks.append(f"### {label}\n" + "\n".join(lines))

    if not blocks:
        return ""

    return "## Company Profile (already known — do not ask the founder to repeat this)\n\n" + "\n\n".join(blocks)
