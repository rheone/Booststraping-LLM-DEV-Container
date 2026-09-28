# Fact Verification Checklist

Every version number, GA/RC date, license term, and feature-availability or deferral claim you
write into a skill needs a search result from the current session behind it before it goes in a
reference file. This is what "verified" means in practice, for a version-gated language-feature
skill and a `dotnet-*` library/platform skill alike.

## What needs verification

- **Version-gated language-feature skills**: the C# language version a feature first shipped in,
  the corresponding .NET SDK version and GA date, and any claim that a feature was *deferred*,
  *previewed then changed*, or *renamed* between versions — these are exactly the claims a language
  model's training data gets wrong, because the true story usually involves a version that didn't
  ship the way an early preview announced.
- **`dotnet-*` library/platform skills**: the package's current stable release version, its license
  (and any recent license-model change — several popular .NET libraries have moved to dual/
  commercial licensing in ways that predate a model's training cutoff), and its minimum supported
  .NET/C# version.
- Whether the current latest version in scope is GA or still RC/preview, and (if RC/preview) the
  expected GA date and whether it carries a go-live license.
- Any specific syntax or API claim tied to a version number (a constraint keyword, an attribute, a
  method signature) — verify the example itself compiles/runs under that version's rules, not just
  that the feature or member exists.

## Search strategy

Query the feature name plus the version number plus a status word: `"C# 13 extension members
preview deferred"`, `".NET 11 release candidate date"`. Run a second query for the adjacent
version on both sides (what shipped just before, what's rumored/previewed just after) — the
"deferred from 13 to 14" shape of many real C# features only surfaces when you check the version
before the one you assumed was first.

## Source preference

When sources disagree (common for anything still in preview), prefer in this order: the
`dotnet/docs` repo or `learn.microsoft.com/dotnet/csharp/whats-new`, official `.NET Blog`
(devblogs.microsoft.com/dotnet) posts, `dotnet/announcements` GitHub issues — over third-party
blogs, which are useful for worked examples but not for pinning a version number or date.

## How to report a license, once verified

Verifying a license and writing about it are different acts — verify every license, but write
about it only when it's non-standard:

- **A standard, unconditionally free license** (MIT, Apache-2.0, BSD-2/3-Clause, and similar
  permissive terms with no commercial tier, revenue threshold, or paid component) gets **no
  commentary at all** — not a "Licensing" section, not a parenthetical in the description, not a
  bare mention of the license name. A reader can assume "no license concern" is the default; only
  write about it when that default doesn't hold.
- **A non-standard license** (dual-licensed with a paid tier, a subscription/commercial edition, a
  revenue-threshold clause, a maintenance-fee request layered on the code license, or any other
  term a reader wouldn't expect from an ordinary open-source dependency) gets exactly **one line in
  the skill's `README.md`**: state that the license is non-standard and that the reader should
  research current terms independently before adopting it. Do not add a dedicated `licensing.md`
  reference file, do not name specific tiers/thresholds/dates/prices, and do not repeat the flag in
  multiple files — license terms change faster than this skill will be revisited, and detailed
  legal summaries go stale (or turn out wrong) in exactly the way that costs a reader real money if
  they're trusted uncritically. A functional API detail that happens to relate to licensing (e.g. a
  `LicenseKey` configuration property that's part of the library's real API surface) is not
  commentary and stays — the rule targets legal explanation, not the code needed to use the
  library.

## The "still an RC" case

A feature whose latest tier is an RC, not GA, needs an explicit caveat in that tier's reference
file — state the RC number, that details may still shift before GA, and the expected GA date —
rather than presenting RC-stage syntax with the same confidence as a shipped version's. The shape:
**"RC caveat:"** a bolded lead-in, the RC number and today's date, and the expected GA date,
immediately under the file's title.

## Completion

Every claim written into a reference or specialized file traces to a search run in the current
session — not a claim you're confident about from training, however familiar it feels. If you
can't currently recall which search backed a specific claim, that claim isn't verified yet; run
it again.
