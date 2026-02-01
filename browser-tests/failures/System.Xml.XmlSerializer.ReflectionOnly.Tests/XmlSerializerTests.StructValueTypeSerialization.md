# XmlSerializerTests Failures Summary

## Test Suite
System.Xml.XmlSerializer.ReflectionOnly.Tests

## Failure Type
Exception - XmlSerializer reflection-based serialization produces default values

## Overview
Multiple XmlSerializer tests fail because struct/value type properties are serialized with default values (0, 0001-01-01, 00:00:00) instead of the actual values set on the object.

## Root Cause
The issue appears to be that XmlSerializer's reflection-based code path on Browser/WASM + CoreCLR interpreter fails to properly read struct property values. The serialization produces XML with default values for all value type properties.

## Affected Tests (15 failures)
1. XmlSerializerTests.Xml_Struct
2. XmlSerializerTests.XML_EnumerableCollection  
3. XmlSerializerTests.Xml_Nullables
4. XmlSerializerTests.Xml_XsdTime_With_TimeOnly_And_DateTime (3 test cases)
5. XmlSerializerTests.Xml_TypeWithDateTimePropertyAsXmlTime
6. XmlSerializerTests.Xml_TypeWithDateOnlyAndTimeOnly
7. XmlSerializerTests.Xml_Soap_WithNullables
8. XmlSerializerTests.Xml_DerivedClasses
9. XmlSerializerTests.XmlMembersMapping_TypeWithXmlAttributes
10. XmlSerializerTests.Xml_TimeOnlyParseErrors
11. XmlSerializerTests.Xml_TypeWithDateTimeOffsetProperty
12. XmlSerializerTests.Xml_TypeWithDefaultTimeSpanProperty
13. XmlSerializerTests.Xml_XsdDate_With_DateOnly_And_DateTime

## Example Failure
```
Test: Xml_Struct
Expected:
  <Some>
    <A>1</A>
    <B>2</B>
  </Some>
Actual:
  <Some>
    <A>0</A>
    <B>0</B>
  </Some>
```

## Notes
- Platform: Browser/WASM + CoreCLR (interpreter mode)
- Category: interpreter - likely related to reflection invoke on value types
- Mono baseline: 297 tests all pass
- CoreCLR: 282 pass, 15 fail, 1 skip
