# Built-In Data Sets

Every generator function passed to `RuleFor` receives a `Faker` instance exposing these data sets as
properties. Each data set groups related generator methods; you pick the data set by what the field
represents, then the specific method by the exact shape you need.

## `Person`

A single `Faker.Person` property (not a method) representing one coherent, internally-consistent
fake person — first name, last name, full name, email, phone, address, gender, and date of birth all
belong to the *same* generated identity:

```csharp
RuleFor(c => c.FullName, f => f.Person.FullName);
RuleFor(c => c.Email, f => f.Person.Email);
```

Reach for `f.Person.X` (rather than the standalone `f.Name`/`f.Internet` equivalents) whenever
several fields on the same object should read as belonging to one real person — a name and an email
generated independently can mismatch (e.g. an email with a different name in it), while `Person`'s
fields are generated together and stay consistent within one call.

## `Name`

Standalone name generation, independent of a full `Person`: `f.Name.FirstName()`,
`f.Name.LastName()`, `f.Name.FullName()`, `f.Name.JobTitle()`, `f.Name.Prefix()`/`f.Name.Suffix()`.

## `Address`

`f.Address.StreetAddress()`, `f.Address.City()`, `f.Address.State()`, `f.Address.ZipCode()`,
`f.Address.Country()`, `f.Address.Latitude()`/`f.Address.Longitude()`. Locale-aware — the format and
plausibility of city/state/zip output depends on the `Faker<T>`'s configured locale (English by
default; Bogus supports many others via a locale code passed to the `Faker<T>` constructor).

## `Commerce`

`f.Commerce.ProductName()`, `f.Commerce.Price()`, `f.Commerce.Department()`,
`f.Commerce.Categories(count)`, `f.Commerce.Ean13()` — retail/product-catalog-shaped data.

## `Internet`

`f.Internet.Email()` (optionally seeded with a first/last name for a matching-looking address),
`f.Internet.UserName()`, `f.Internet.Url()`, `f.Internet.Ip()`/`f.Internet.Ipv6()`,
`f.Internet.Password()`, `f.Internet.DomainName()`.

## `Lorem`

Placeholder text: `f.Lorem.Word()`, `f.Lorem.Sentence()`, `f.Lorem.Paragraph()`,
`f.Lorem.Paragraphs(count)`. Use this for free-text fields (descriptions, comments, notes) where the
actual words don't matter, only that a non-empty, human-text-shaped string is present.

## `Date`

`f.Date.Past(years)`, `f.Date.Future(years)`, `f.Date.Between(start, end)`,
`f.Date.Recent(days)` — all return `DateTime`, with overloads for `DateOnly`/`TimeOnly` in more
recent Bogus versions targeting frameworks that support those types.

## `Finance`

`f.Finance.Amount(min, max)`, `f.Finance.Currency()`, `f.Finance.Account()` (a fake account number),
`f.Finance.CreditCardNumber()`, `f.Finance.Bic()`/`f.Finance.Iban()`.

## `Random` and `PickRandom`

`f.Random` is the lowest-level data set: `f.Random.Int(min, max)`, `f.Random.Guid()`,
`f.Random.Bool()`, `f.Random.ArrayElement(array)`, `f.Random.Double()`. `f.PickRandom<TEnum>()` (a
`Faker`-level extension, not under `f.Random`) picks a random defined value of an enum type;
`f.PickRandom(list)` picks a random element from an existing collection.

## Picking the right data set for a field

Match the data set to what the field represents, not just its .NET type — a `string` field named
`Company` reads better generated from a dataset with company-name generation than from `Lorem`, even
though both produce a `string`. When multiple fields on one object should tell a consistent story
(a person's name and their email, a company and its matching website domain), prefer generating them
from the same coherent source (`Person` for a person's fields) or deriving one from the other inside
the `RuleFor` body, over generating each independently and risking a mismatch.
