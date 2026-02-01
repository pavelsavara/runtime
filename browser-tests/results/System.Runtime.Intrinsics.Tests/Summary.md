# System.Runtime.Intrinsics.Tests - Browser/WASM CoreCLR Test Summary

## Test Results

| Metric | CoreCLR | Mono Baseline |
|--------|---------|---------------|
| Tests Run | 12756 | 12805 |
| Passed | 12756 | 12805 |
| Failed | 0 | 0 |
| Skipped | 81 | 0 |

## Status: ⚠️ Tests with ActiveIssue

## Changes Made

### ActiveIssue Attributes Added

1. **Vector64Tests.GetOne* (12 tests)** - [Vector64-128-256Tests.GetOne.md](../../failures/System.Runtime.Intrinsics.Tests/Vector64-128-256Tests.GetOne.md)
   - `GetOneByte`, `GetOneDouble`, `GetOneInt16`, `GetOneInt32`, `GetOneInt64`, `GetOneIntPtr`, `GetOneSByte`, `GetOneSingle`, `GetOneUInt16`, `GetOneUInt32`, `GetOneUInt64`, `GetOneUIntPtr`
   - Issue: `Vector64<T>.One` returns zero vector instead of vector filled with 1s on CoreCLR interpreter

2. **Vector128Tests.GetOne* (12 tests)** - Same issue as Vector64
   
3. **Vector256Tests.GetOne* (12 tests)** - Same issue as Vector64

4. **PackedSimdTests (45 tests)** - [PackedSimdTests.md](../../failures/System.Runtime.Intrinsics.Tests/PackedSimdTests.md)
   - Entire class marked with `[ActiveIssue]`
   - Issue: `PackedSimd.IsSupported` returns `false` on CoreCLR interpreter (WASM SIMD intrinsics not supported)

## Test Comparison

- **Extra in CoreCLR (15 tests)**: ConvertToInt32Test, ConvertToInt64Test, etc. - Tests that have `[SkipOnMono]` attributes
- **Missing in CoreCLR (49 tests)**: All PackedSimdTests - skipped due to ActiveIssue

## Notes

- CoreCLR interpreter does not support WASM SIMD hardware intrinsics (PackedSimd)
- There appears to be a bug in the CoreCLR interpreter with `Vector<T>.One` property returning zero vectors
- All previously-passing tests continue to pass
