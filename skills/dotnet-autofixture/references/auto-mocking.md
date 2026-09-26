# Auto-mocking integration

## The problem: AutoFixture can't construct an interface on its own

`Create<T>()` works by reflecting over a concrete constructor. Given an interface or an abstract
class, there's no constructor to call — a plain `Fixture` throws
`ObjectCreationException` for `fixture.Create<ISomeInterface>()` unless something tells it how to
produce a value for that type instead.

## The general pattern: a customization that delegates to a mocking library

The standard fix is a customization (see `references/customizations.md`) that recognizes "this
requested type has no concrete constructor" and, instead of failing, asks a mocking library to
generate a dynamic proxy/mock for it, then hands that back as the created value. Conceptually:

```csharp
public class AutoMockCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new MockRelay());
        // "MockRelay" stands for whatever specifier your chosen mocking library's
        // AutoFixture integration package provides — it intercepts requests for
        // interfaces/abstract types and produces a mock instead of failing.
    }
}
```

Once this kind of customization is applied, `fixture.Create<IOrderRepository>()` returns a
dynamically generated mock of `IOrderRepository` instead of throwing — and, critically, when
`Create<T>()` builds a concrete class whose constructor takes an interface parameter, that
parameter is filled with an auto-generated mock too, without the test having to construct it by
hand.

```csharp
// OrderService's constructor takes IOrderRepository and IClock — both interfaces.
// With auto-mocking applied, this line builds an OrderService with both dependencies
// automatically supplied as mocks, none of them written out by the test.
var sut = fixture.Create<OrderService>();
```

## Retrieving the mock to configure or assert on it

A test usually still needs to configure a specific mock's behavior or assert calls made on it. Ask
the fixture to freeze or re-resolve the same interface type — most mocking-library AutoFixture
integrations return the *same* mock instance for repeated requests of the same interface type
within one `Fixture`, so requesting it again retrieves the instance already wired into the object
under test:

```csharp
var repositoryMock = fixture.Freeze<IOrderRepository>(); // pins/retrieves the shared instance
// configure repositoryMock's behavior via your mocking library's own API before Create<T>()
var sut = fixture.Create<OrderService>();
// ...
// assert on repositoryMock via your mocking library's own verification API
```

`Freeze<T>()` (a core `Fixture` method, independent of auto-mocking) makes every subsequent request
for `T` within that fixture return the same instance rather than a fresh one each time — the
mechanism that makes "configure the mock, then build the object that depends on it" work in either
order.

## Choosing and wiring the integration

Which package provides `AutoMockCustomization`-equivalent behavior depends on which mocking library
a project already uses — check that mocking library's own package family for an AutoFixture
integration package (commonly named along the lines of `AutoFixture.Auto<MockingLibrary>`) and
apply its customization the same way any other `ICustomization` is applied:

```csharp
var fixture = new Fixture().Customize(new AutoMockCustomization()); // from that integration package
```

This skill describes the pattern generically because the specific package name and configuration
surface belong to whichever mocking library a given project has chosen — verify the current package
name, version, and API against that library's own documentation before wiring it in.
