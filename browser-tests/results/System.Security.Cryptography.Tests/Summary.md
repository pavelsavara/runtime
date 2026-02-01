# System.Security.Cryptography.Tests Summary

## Test Results

| Runtime | Tests Run | Passed | Failed | Skipped |
|---------|-----------|--------|--------|---------|
| **CoreCLR** | 4902 | 4103 | 0 | 799 |
| **Mono** | 4877 | 4078 | 0 | 799 |

## Status: ✅ PASSING

All tests pass. CoreCLR runs 25 more tests than Mono (likely tests with `[SkipOnMono]` that now run on CoreCLR).

## Analysis

- **XHarness exit code**: 0
- **Extra tests in CoreCLR**: 25 (tests skipped on Mono but enabled for CoreCLR)
- **No failures**
- **No timeouts or crashes**

## Test Comparison

CoreCLR runs more tests than Mono baseline because some tests are marked `[SkipOnMono]` but run on CoreCLR.

## Notes

- Date: 2026-02-01
- Configuration: Release
- Platform: Browser/WASM + CoreCLR
