"""Phase 3 "Dynamic Work" — a lightweight, deterministic keyword router that picks a
single Founder specialist for a focused operational request ("Create Instagram
content", "Should I increase prices?", "Who is my biggest competitor?"), instead of
re-running the full 11-agent venture-framing DAG for every request once a business
already exists.

Deliberately not an LLM classifier: this runs once per intake and must behave
identically with or without a configured model provider (the mock fallback doesn't do
real classification), so a fast, explainable, fully offline keyword match is the right
tool — see SupervisorAgent.kickoff for where "nothing scored" falls back to the full
venture DAG rather than guessing at a single agent.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class FounderRoute:
    node_name: str
    task_type: str
    keywords: tuple[str, ...]


ROUTES: tuple[FounderRoute, ...] = (
    FounderRoute(
        "VentureFraming", "FrameVenture",
        ("vision", "pitch", "investor", "overall strategy", "executive summary", "elevator pitch"),
    ),
    FounderRoute(
        "BusinessModelAnalysis", "AnalyzeBusinessModel",
        ("business model", "revenue model", "value proposition", "swot"),
    ),
    FounderRoute(
        "MarketResearch", "ResearchMarket",
        ("market", "competitor", "market size", "industry trend", "biggest competitor"),
    ),
    FounderRoute(
        "CustomerResearch", "ResearchCustomers",
        ("customer", "persona", "target audience", "buyer"),
    ),
    FounderRoute(
        "BrandStrategy", "DefineBrandIdentity",
        ("brand", "logo", "slogan", "identity", "design", "collection", "mission statement", "vision statement"),
    ),
    FounderRoute(
        "FinancialPlanning", "ProjectFinancials",
        ("budget", "manufacturing cost", "cash flow", "financial", "increase price", "decrease price",
         "raise price", "funding", "revenue goal", "cost estimate"),
    ),
    FounderRoute(
        "MarketingPlanning", "CreateMarketingPlan",
        ("marketing", "instagram", "social media", "campaign", "content calendar", "advertis",
         "black friday", "promotion", "content for"),
    ),
    FounderRoute(
        "OperationsPlanning", "PlanOperations",
        ("supplier", "shipping", "inventory", "fulfillment", "manufactur"),
    ),
    FounderRoute(
        "SalesPlanning", "DefineSalesStrategy",
        ("sales strategy", "pricing strategy", "discount", "sales channel"),
    ),
    FounderRoute(
        "GrowthPlanning", "PlanGrowth",
        ("growth", "expand", "expansion", "scale", "new market", "new country"),
    ),
    FounderRoute(
        "LegalReview", "ReviewLegalRisk",
        ("legal", "contract", "compliance", "trademark", "risk assessment"),
    ),
)


def route_single_agent(raw_input: str) -> FounderRoute | None:
    """Best-scoring route, or None if nothing matched — callers should fall back to
    the full venture DAG for a broad/ambiguous request rather than guessing."""
    text = raw_input.lower()
    best: FounderRoute | None = None
    best_score = 0
    for route in ROUTES:
        score = sum(1 for kw in route.keywords if kw in text)
        if score > best_score:
            best_score = score
            best = route
    return best
