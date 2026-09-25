# Licensing (read before adopting)

AutoMapper's license changed materially in 2025. This is not a corner case — it affects whether a
company can legally use current versions of the package at all, so check it before recommending
AutoMapper or before upgrading an existing dependency.

## The short version

- **AutoMapper versions before v15.0** are MIT-licensed, permissive, free for any use. Nothing
  about the license change is retroactive — if a project is pinned to, say, v12.x or v13.x, it
  keeps its MIT rights on that version forever.
- **AutoMapper v15.0 and later** ship under a **dual license**:
  1. **Reciprocal Public License 1.5 (RPL 1.5)** — a copyleft open-source license, free to use,
     but with reciprocal (share-alike) obligations more restrictive than MIT/Apache. This is the
     "Community Edition" path.
  2. **A paid commercial subscription**, sold by Lucky Penny Software (Jimmy Bogard's company),
     which removes the RPL 1.5 copyleft obligations.

## Who qualifies for free use

Per the announced Community Edition terms, the free RPL 1.5 tier applies to:

- Individuals and companies with **less than $5M in annual gross revenue**.
- Non-profits with budgets under $5M annually.
- Educational/classroom use.
- Non-production environments (e.g. internal prototypes, local dev) regardless of company size.

Free use still requires **registering a free license key** for auditing purposes — it is not a
no-strings MIT-style free-for-all; it's "free, but declared."

## Who has to pay

Companies/individuals **above the $5M annual gross revenue threshold** using AutoMapper v15.0+ in
production need a paid commercial subscription. Pricing is tiered by developer headcount:

| Tier | Developers |
| --- | --- |
| Standard | 1–10 |
| Professional | 11–50 |
| Enterprise | Unlimited |

Subscriptions are sold monthly or annually through Paddle (payment processor), via
**luckypennysoftware.com**. Bundling a subscription with MediatR (Bogard's other library, which
underwent the same licensing change) is discounted.

## How the license key works in code

v15.0+ requires an explicit license key to be configured, or the library will otherwise announce
it is running unlicensed:

```csharp
services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = "<license key here>";
}, typeof(SomeProfile));
```

Both the free Community Edition key and a paid key are supplied the same way — the difference is
which key you registered for and whether your usage qualifies for the free tier.

## Practical guidance for adoption decisions

- **New project, small company, well under $5M revenue**: the free tier applies; register a key
  and proceed, understanding RPL 1.5's copyleft terms are stricter than MIT (read the actual RPL
  1.5 text for the specifics of what "reciprocal" requires before shipping a closed-source
  product — this is a legal question a mapping-conventions skill cannot answer for you).
- **New project, large/enterprise company**: budget for a commercial subscription, or evaluate
  whether the value AutoMapper adds (see
  [references/pitfalls-and-alternatives.md](references/pitfalls-and-alternatives.md)) justifies
  the recurring cost versus a source-generator alternative or manual mapping.
- **Existing project already pinned below v15.0**: no forced action — the license on the pinned
  version doesn't change. But staying pinned forever means missing bug fixes and new features;
  factor that into the "just stay on the old version" plan.
- **Any project**: this is a licensing/legal decision, not a purely technical one. Flag it
  explicitly rather than silently adding or upgrading the package in a codebase.

## Why this happened (context, not actionable)

The maintainer (Jimmy Bogard) cited unsustainable maintenance economics: after moving to
freelance consulting, the indirect funding that supported maintaining AutoMapper and MediatR as
free OSS projects no longer existed, which was slowing bug fixes and feature work. The commercial
model funds continued maintenance. This is mentioned only so the "why" doesn't read as arbitrary
if it comes up in discussion — it doesn't change the adoption calculus above.
