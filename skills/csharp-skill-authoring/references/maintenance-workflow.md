# Maintenance Workflow

Extending an existing version-gated C# skill with a newly shipped C# version's tier. Steps 1–2 of
[SKILL.md](../SKILL.md) still apply in full — scope what changed, verify it against a current
search — before anything here.

## 1. Identify what the new version actually adds to this skill's domain

A new C# version rarely touches every skill's feature. Search specifically for "what's new in
C#N" filtered to this skill's feature area, not just the general release notes — most of a
release's content is irrelevant to any one skill.

## 2. Decide: new tier, or fold into an existing file?

A new `references/` file is warranted only when *behavior or availability* actually changed at
this version — a new constraint kind, a new block syntax, a capability that didn't exist a
version ago. A small addition that doesn't change what compiles or how the feature is used (a
new BCL type implementing an existing interface, a performance improvement) belongs as a short
addition inside the *current* newest tier's file, not a new one.

## 3. Add the new reference file, if warranted

From [assets/templates/reference-file.md.tmpl](../assets/templates/reference-file.md.tmpl). Its
Fallback section names the tier that was previously newest — that tier's own Fallback section
still names the one before it, so the chain stays intact without editing every earlier file.

## 4. Update both routing tables — and re-check the row above the new one

Add the new version's row to `SKILL.md`'s "Pick your reference file" table and `README.md`'s
"Version coverage" table. Then re-check the row that was previously newest: if it was written
with RC-stage caveats ("RC1 as of [date], GA expected [month]"), and the new version's arrival
means that RC actually shipped, correct its status and date rather than leaving a stale caveat
sitting one row above the new addition. A routing table with two consecutive "still RC" rows,
one of which has since GA'd, is a symptom of a maintenance pass that only appended and never
re-checked.

## 5. Re-check forward-looking statements in every existing file

Skim `references/` and `specialized/` for sentences that assert something about *future*
versions — "C#N adds nothing new here," "no later version changes this," a Fallback section that
says "this is the newest tier." These are exactly the lines a new version's arrival can turn
false without anyone editing them, since nothing about adding a new file forces a re-read of the
old ones. Grep for phrasing like "nothing new," "no new," "as of," and "newest tier" across the
skill's existing files as a mechanical check before considering the maintenance pass complete.

## Completion

The maintenance pass is done when: the new tier (if warranted) exists and fallback-chains
correctly; both routing tables include it; the row that was previously newest reflects its
current (not stale) status; and no existing file still asserts something about "the newest
version" that the new addition just made false.
