# C# Decorator Pattern

Reference for the Decorator design pattern in C#: wrapping an interface implementation to add
behavior without modifying it, a generic decorator base class for wide interfaces, chaining
multiple decorators, and how decorator composition compares in intent to a DI container's own
pipeline/interceptor-style features. The routing table is in [SKILL.md](SKILL.md).

**`references/`**

| File | Covers |
| --- | --- |
| `classic-decorator.md` | wrapping an interface to add behavior; composing decorators; decorator vs. inheritance |
| `generic-decorator-base.md` | a generic base class forwarding every interface member; why forwarding members must be `virtual` |
| `chaining-decorators.md` | building a chain, why order changes observable behavior, assembling a chain from configuration |
| `decorator-vs-di-pipelines.md` | the shared intent and the differences between hand-written chains and a container's own wrapping mechanism |
| `testing-decorators.md` | testing forwarding, failure-reacting decorators, and emergent behavior in an assembled chain |
| `extending-decorators.md` | adding a new decorator without touching existing ones; invariants that keep the composition safe |
