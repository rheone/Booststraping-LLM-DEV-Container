/write-a-skill

You are a technical writer and software developer

Create a LLM / Claude / open code skill that is capable of reverse engineering an existing software project from its source code.
This skill should also be capable of updating its documents top down, or on a per domain level.

It should provide:

- an overview of the project
  - create a glossary of uncommon terms
- document each domain
- document each feature
- include the "what" not "why"
- each document's relevant sections should include a confidence score of "High", "Medium", and "Low"
  - include a one-line evidence pointer for anything below High (e.g., Confidence: `Medium — inferred from handler naming; no tests exercise this path).` 
- Information may be inferred from tests; but explicitly mark the source pointing to the evidence

Scope: behavior overview first; suggest a list of domains and continue; and finally features until the user stops you or all expected documents exist

Include a batch mode that creates/updates all documents that allows the user to walk away and complete all tasks
Use sub agents when possible to document without additional input. Compress context as necessary

Write the results to a properly nested folder structure

```
docs/reverse-engineered-docs/
     overview.md
     domains/
       <domain-slug>.md
       <domain-slug>/
         features/
           <feature-slug>.md
     _meta/
       glossary.md
       open-questions.md       # things the skill couldn't determine
       confidence-summary.md
```

DDD context may span multiple libraries and dependencies in the code base. Often, for example all the actions in controller, or API endpoints that act on the same entity
Inferred from behavior, not folders.

Confidence levels — format. I'd propose categorical High / Medium / Low with a one-line evidence pointer for anything below High (e.g., Confidence: Medium — inferred from handler naming; no tests exercise this path). Numeric percentages tend to imply false precision. Acceptable, or do you want something else?

The audience for the documents are senior software developers

The audience for the documentation is senior software developer.

Do you have any questions or additional recommendations before we start?