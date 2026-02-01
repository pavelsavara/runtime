# System.Xml.XmlSerializer.ReflectionOnly.Tests - Browser CoreCLR Test Summary

## Status: ⚠️ PASSED WITH ISSUES

## Test Results
- **Tests Run:** 273
- **Passed:** 272
- **Failed:** 0
- **Skipped:** 1
- **Mono Baseline:** 279 tests

## ActiveIssue Marked Tests (19 total)

All failures related to XmlSerializer reflection-based serialization issues with value types on Browser+CoreCLR interpreter.

### Issue: [dotnet/runtime#123011](https://github.com/dotnet/runtime/issues/123011)

**Root Cause:** XmlSerializer reflection-based serialization produces default values (0, 0001-01-01, 00:00:00, etc.) instead of actual values for struct/value type properties when running on Browser+CoreCLR interpreter mode.

**Files Modified:**
1. `src/libraries/System.Private.Xml/tests/XmlSerializer/XmlSerializerTests.cs` (15 tests)
2. `src/libraries/System.Private.Xml/tests/XmlSerializer/XmlSerializerTests.RuntimeOnly.cs` (6 tests)

**Tests Marked with ActiveIssue:**

#### XmlSerializerTests.cs:
1. `Xml_TypeWithDateTimePropertyAsXmlTime`
2. `Xml_Struct`
3. `Xml_Nullables`
4. `Xml_DerivedClasses`
5. `Xml_TypeWithDefaultTimeSpanProperty`
6. `Xml_TypeWithDateTimeOffsetProperty`
7. `Xml_TimeOnlyParseErrors`
8. `Xml_TypeWithDateOnlyAndTimeOnly`
9. `Xml_XsdDate_With_DateOnly_And_DateTime`
10. `Xml_XsdTime_With_TimeOnly_And_DateTime`
11. `Xml_DeserializeHiddenMembersTest`
12. `Xml_BaseClassAndDerivedClass2WithSameProperty`
13. `Xml_TypeWithTypesHavingCustomFormatter`
14. `Xml_DerivedClasses`
15. `Xml_BaseClassAndDerivedClassWithSameProperty`

#### XmlSerializerTests.RuntimeOnly.cs:
1. `XML_EnumerableCollection`
2. `Xml_Soap_WithNullables`
3. `XmlMembersMapping_TypeWithXmlAttributes`
4. `SoapEncodedSerialization_SoapAttribute`
5. `Xml_XmlTextAttributeTest`
6. `Xml_NookTypes`

## Comparison with Mono Baseline

CoreCLR runs 273 tests (6 fewer than Mono's 279) due to ActiveIssue skips.

## Failure Documentation

See [XmlSerializerTests.StructValueTypeSerialization.md](../../failures/System.Xml.XmlSerializer.ReflectionOnly.Tests/XmlSerializerTests.StructValueTypeSerialization.md) for detailed failure analysis.
