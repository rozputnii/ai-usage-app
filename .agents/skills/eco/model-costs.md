# Model costs

Evidence for the eco role map (decision R-NEW).

- checked_at: 2026-10-10
- sources: [Anthropic pricing](https://platform.claude.com/docs/en/about-claude/pricing),
  [Artificial Analysis, Opus 5.5 vs Sonnet 5.5](https://artificialanalysis.ai/models/releases/comparisons/claude-opus-5-5-vs-claude-sonnet-5-5),
  [Artificial Analysis, Haiku 5.5 vs Sonnet 5.5](https://artificialanalysis.ai/models/releases/comparisons/claude-haiku-5-5-vs-claude-sonnet-5-5),
  [Anthropic, Claude Haiku 5.5](https://www.anthropic.com/claude-haiku-5-5)

## Prices per million tokens

| Model | Input | Output | Cache read |
| --- | --- | --- | --- |
| Opus 5.5 | $4 | $20 | $0.20 |
| Sonnet 5.5 | $2 | $10 | $0.10 |
| Haiku 5.5, prompt up to 100k | $0.10 | $0.50 | $0.01 |
| Haiku 5.5, prompt over 100k | $0.50 | $2.50 | $0.05 |

Opus 5.5 and Sonnet 5.5 have no long-context premium.

## Quality and cost per task

Index is the Artificial Analysis Intelligence Index, TB4 is Terminal-Bench 4.0, and cost is
per Index task. They come from a general test mix, not this repository's workload.

| Model | Effort | Index | TB4 | Cost |
| --- | --- | --- | --- | --- |
| Opus 5.5 | low | 42 | 31.3% | $0.55 |
| Opus 5.5 | medium | 51 | 52.5% | $1.34 |
| Opus 5.5 | high | 54 | 56.6% | $1.82 |
| Opus 5.5 | xhigh | 56 | 59.6% | $3.46 |
| Opus 5.5 | max | 58 | 59.6% | $5.98 |
| Sonnet 5.5 | low | 36 | 20.7% | $0.35 |
| Sonnet 5.5 | medium | 41 | 29.8% | $0.48 |
| Sonnet 5.5 | high | 47 | 43.9% | $0.88 |
| Sonnet 5.5 | xhigh | 52 | 57.1% | $2.01 |
| Sonnet 5.5 | max | 56 | 63.6% | $5.46 |
| Haiku 5.5 | low | 29 | 12.6% | $0.02 |
| Haiku 5.5 | medium | 34 | 15.2% | $0.05 |
| Haiku 5.5 | high | 38 | 21.7% | $0.08 |
| Haiku 5.5 | xhigh | 41 | 29.3% | $0.12 |
| Haiku 5.5 | max | 43 | 32.8% | $0.21 |

## Reading

- Haiku 5.5 at max beats Sonnet 5.5 at low and medium on both scores for less money, and
  Haiku at xhigh roughly ties Sonnet at medium for a quarter of the cost, so Sonnet below high
  is never the pick. Anthropic positions Haiku for subagents and summaries, not complex agentic
  coding, so the map uses it for exploration and check runs.
- Opus 5.5 at medium costs about 2.8 times Sonnet 5.5 at medium per task, but comes within a
  point of Sonnet at xhigh on the Index (51 against 52) for two thirds of the cost, though
  lower on TB4. It is the default for implementation and ordinary reviews.
- Sonnet 5.5 at high is the cheapest step near that level and suits small, fully specified
  briefs.
- Opus 5.5 at high stays the gate for T3 reviews.
