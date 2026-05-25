# General Quality Checklist

Apply at every class regardless of test framework. Step 2 of the sweep loop references this checklist.

- [ ] Use "object mothers" when repeatedly using similar data for testing purposes
- [ ] Test names clearly state: member under test, scenario, expected outcome -- {MemberUnderTest}_{Scenario}_{Expectations}_Test
- [ ] Each test follows Arrange / Act / Assert with section comments
- [ ] A parameterized test with exactly one data row is a violation — convert to non-parameterized test
- [ ] Each set of test cases covers at minimum: one happy-path case, one failure/null/invalid path, and relevant boundary values
- [ ] Strongly-typed data sources used where the framework supports it; avoid raw `object[]` arrays
- [ ] Parse-roundtrip data: when theory row inputs feed a parse API, derive them from the same normalized form the expected value uses — never from a raw pre-normalization source
- [ ] Loop-generated theory data: when iterating a source set through a normalizing constructor, prefer reducing the source set to one canonical value per equivalence class (**source reduction**); use a `HashSet<string>` keyed on the serialized form only as a fallback when the full cross-product is genuinely needed for coverage
- [ ] Exception tests use the framework's assertion helper, not try/catch
- [ ] Async tests return correct type (not `void`); cancellation tokens passed explicitly where applicable
- [ ] No shared static mutable state between tests
- [ ] Never mock/substitute the class under test — mocking frameworks intercept virtual methods, causing the method under test to return a default value instead of executing. Use a concrete subclass instead. (Applies to all frameworks: NSubstitute `Substitute.For<T>`, Moq `new Mock<T>`, JustMock `Mock.Create<T>`, RhinoMocks `MockRepository.GenerateMock<T>`.)
- [ ] Mock/substitute call assertions belong in the Assert section only
- [ ] Disabled/skipped tests include written justification
- [ ] Test output helpers used only for diagnostic context, not as assertion substitutes
- [ ] Every test method contains at least one assertion (`Assert.*`, `Received()`, `Verify()`, or `Assert.Throws`) — tests with no assertions pass vacuously and provide false confidence
- [ ] Any test using a mock, stub, or expectation should assert expectations
- [ ] Each test verifies one logical scenario (not combining unrelated behaviors)
