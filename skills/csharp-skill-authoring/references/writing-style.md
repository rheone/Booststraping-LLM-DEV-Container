# Writing Style

Every file you write is an instruction set for an agent to follow, not a narrative about how the
skill came to be. Hold to this voice throughout:

- **Present tense, active voice, second person.** The agent is the "you" doing the thing, right
  now: "You register the handler" rather than "The handler is registered" or "The handler would be
  registered." "You do this" rather than "This should be done" or "This will be done."
- **No delta-narrative.** Never describe a file, a rule, or a piece of guidance in terms of what it
  *used to say*, what changed since a previous version, or what a prior draft got wrong. Write the
  current, final instruction as if it always read this way. A rule that was "retired" or
  "previously recommended X, now Y" belongs in a commit message or changelog, never in the skill
  body — the agent reading the file has no prior version to contrast against, so the contrast is
  pure noise that never resolves to an instruction.
- **State the rule, not its history or its rationale-as-story.** "State the scope boundary on its
  own terms" is an instruction; "we used to let files reference each other but that caused
  problems so now we don't" is a story. Keep the *why* only when it changes how the agent applies
  the rule at the edges (a genuine judgment aid), and phrase even that as a standing fact ("a
  narrow, deep domain of its own with different failure modes"), not as an event in the past.

## Self-check

Before finishing any file, scan it for: past-tense verbs describing the skill's own guidance
("was," "used to," "previously," "no longer," "now instead"), third-person framing of the agent
("the user should," "one would"), and any sentence whose sole content is contrasting this version
against an earlier one. Rewrite each hit as a direct, present-tense instruction.
