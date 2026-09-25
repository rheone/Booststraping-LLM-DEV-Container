# Fact Verification Checklist

The hard requirement from Step 2 of [SKILL.md](../SKILL.md): every version number, GA/RC date,
and feature-availability or deferral claim needs a search result from the current session behind
it before it goes in a reference file. This is what "verified" means in practice.

## What needs verification

- The C# language version a feature first shipped in, and the corresponding .NET SDK version and
  GA date.
- Any claim that a feature was *deferred*, *previewed then changed*, or *renamed* between
  versions — these are exactly the claims a language model's training data gets wrong, because
  the true story usually involves a version that didn't ship the way an early preview announced.
- Whether the current latest version in scope is GA or still RC/preview, and (if RC/preview) the
  expected GA date and whether it carries a go-live license.
- Any specific syntax claim tied to a version number (a constraint keyword, an attribute, a
  block syntax) — verify the syntax example itself compiles under that version's rules, not just
  that the feature exists.

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

## The "still an RC" case

A feature whose latest tier is an RC, not GA, needs an explicit caveat in that tier's reference
file — state the RC number, that details may still shift before GA, and the expected GA date —
rather than presenting RC-stage syntax with the same confidence as a shipped version's. See
`csharp-extension-members`' `csharp15-extension-indexers.md` for the caveat's wording, if that
skill is installed; otherwise, the shape is: **"RC caveat:"** a bolded lead-in, the RC number and
today's date, and the expected GA date, immediately under the file's title.

## Completion

Every claim written into a reference or specialized file traces to a search run in the current
session — not a claim you're confident about from training, however familiar it feels. If you
can't currently recall which search backed a specific claim, that claim isn't verified yet; run
it again.
