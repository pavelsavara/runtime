# System.Runtime.Serialization.Schema.Tests - Browser WASM Test Results

## Test Suite Information
- **Test Project**: System.Runtime.Serialization.Schema.Tests
- **Project Path**: src/libraries/System.Runtime.Serialization.Schema/tests/System.Runtime.Serialization.Schema.Tests.csproj
- **Test Date**: 2026-02-01 16:59

## Results Summary
| Metric | Count |
|--------|-------|
| Total Tests Run | 114 |
| Passed | 114 |
| Failed | 0 |
| Skipped | 0 |

## Test Comparison (vs Mono Baseline)
| Metric | Count |
|--------|-------|
| Mono Tests | ~114 |
| CoreCLR Tests | 114 |
| Extra in CoreCLR | 5 |
| Missing in CoreCLR | 9 |

## Status: ✅ PASSED

All 114 tests passed successfully. The comparison differences are due to XmlSchemaObjectTable ToString representation differences between Mono and CoreCLR (throwing different exception types in the representation) - not actual test differences.
