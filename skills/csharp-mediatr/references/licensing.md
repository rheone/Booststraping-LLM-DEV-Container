# Licensing (read before adopting)

MediatR's license changed materially in 2025, in lockstep with the same maintainer's other
library. This is not a corner case — it affects whether a company can legally use current versions
of the package at all, so check it before recommending MediatR or before upgrading an existing
dependency.

## The short version

- **MediatR v12.5.0 and earlier** are Apache 2.0-licensed, permissive, free for any use. Nothing
  about the license change is retroactive — if a project is pinned to, say, v11.x or v12.x, it
  keeps its Apache 2.0 rights on that version forever. v12.5.0 is explicitly called out by the
  maintainer as the last release under the original license.
- **MediatR v13.0 and later** ship under a **dual license**:
  1. **Reciprocal Public License 1.5 (RPL 1.5)** — a copyleft open-source license, free to use,
     but with reciprocal (share-alike) obligations more restrictive than Apache/MIT. This is the
     "Community Edition" path.
  2. **A paid commercial subscription**, sold by Lucky Penny Software (Jimmy Bogard's company,
     the same company and the same license structure applied to his other mapping library), which
     removes the RPL 1.5 copyleft obligations.
- The repository itself moved: MediatR now lives under the `LuckyPennySoftware` GitHub
  organization rather than the original `jbogard` personal account, reflecting the same corporate
  restructuring behind the license change.
- The commercial edition launched **July 2, 2025**.

## Who qualifies for free use

Per the Lucky Penny Software licensing FAQ, the free RPL 1.5 "Community" tier applies to
individuals and organizations that meet **all** of the following:

- **Annual gross revenue under $5,000,000 USD**.
- **Have not received more than $10,000,000 USD in outside capital** (VC/PE funding, etc.).
- **Not a government, quasi-government, or higher-education entity** using the library for
  institutional operations. Classroom/research educational use qualifies regardless of the type
  of institution.
- Non-profits under the same revenue threshold qualify.
- Non-production environments (internal prototypes, local dev) qualify regardless of
  organization size or revenue.

Free use under the Community tier does not require a paid key, but you are still bound by the
RPL 1.5 terms — it is "free, but with copyleft/reciprocal obligations," not a no-strings
Apache-style free-for-all.

## Who has to pay

Organizations that fail any of the Community-tier qualifications above (most commonly: **over
the $5M annual gross revenue threshold**, or over $10M in outside capital raised) using MediatR
v13.0+ in production need a paid commercial subscription. Pricing is tiered by developer
headcount — specifically, developers "actively writing or maintaining code that uses the
library" (this includes contractors and developers at affiliated companies working on your
product):

| Tier | Developers |
| --- | --- |
| Standard | 1–10 |
| Professional | 11–50 |
| Enterprise | Unlimited |

Tiers are **non-cumulative** — exceeding a tier's headcount means upgrading to the next tier, not
buying additional per-seat add-ons. Subscriptions are sold monthly (12-month minimum commitment)
or annually (priced at ten months' worth of the monthly fee) via **luckypennysoftware.com**,
through direct checkout, quote-and-invoice (ACH/wire supported), or authorized resellers.
Bundling a subscription with the maintainer's other mapping library is discounted.

## How the license key works in code

v13.0+ resolves a license key from, in order of precedence:

1. An explicit value set in code (`cfg.LicenseKey` in `AddMediatR`, or `Mediator.LicenseKey`
   directly).
2. The `MEDIATR_LICENSE_KEY` environment variable.
3. The `LUCKYPENNY_LICENSE_KEY` environment variable (shared across Lucky Penny Software
   products, so one variable can cover multiple libraries from the same vendor).

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.LicenseKey = "<license key here>";
});
```

Both the free Community Edition key and a paid key are registered and supplied the same way — the
difference is which key you registered for at MediatR's licensing portal and whether your usage
qualifies for the free tier. Key points on enforcement:

- **License key enforcement is entirely self-contained in the library** — it does not phone home
  to a license server at runtime.
- **Client-side/WASM applications do not need a key set** (e.g. a Blazor WebAssembly front end
  calling into a server that itself is properly licensed).
- If no valid key is present, the library logs a warning but **does not stop the application from
  running** — there's no hard runtime failure, only a nag. The warning can be suppressed via
  logging configuration (`builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License",
  LogLevel.None)`), but suppressing the warning does not resolve the underlying licensing
  obligation — it only silences the notice.

## Practical guidance for adoption decisions

- **New project, small company, well under $5M revenue and under $10M raised**: the free
  Community tier applies; register a key (or set the env var) and proceed, understanding RPL 1.5's
  copyleft terms are stricter than Apache/MIT — read the actual RPL 1.5 text for the specifics of
  what "reciprocal" requires before shipping a closed-source product. This is a legal question a
  messaging-pattern skill cannot answer for you.
- **New project, large/enterprise company or VC-backed past the outside-capital threshold**:
  budget for a commercial subscription, or evaluate whether MediatR's indirection benefit (see
  [references/pitfalls-and-tradeoffs.md](references/pitfalls-and-tradeoffs.md)) justifies the
  recurring cost versus writing the mediator/dispatch pattern by hand — it is a small enough
  pattern that a hand-rolled version is a realistic fallback, unlike a full mapping engine.
- **Existing project already pinned at or below v12.5.0**: no forced action — the license on the
  pinned version doesn't change. But staying pinned forever means missing bug fixes and any new
  feature work; factor that into the "just stay on the old version" plan.
- **Any project**: this is a licensing/legal decision, not a purely technical one. Flag it
  explicitly rather than silently adding or upgrading the package in a codebase.

## Why this happened (context, not actionable)

The maintainer (Jimmy Bogard) cited unsustainable maintenance economics: after moving to
freelance/independent work, the indirect funding that had supported maintaining MediatR and his
other library as free OSS projects no longer existed, which was slowing bug fixes and feature
work. The commercial model funds continued maintenance. This is mentioned only so the "why"
doesn't read as arbitrary if it comes up in discussion — it doesn't change the adoption calculus
above.
