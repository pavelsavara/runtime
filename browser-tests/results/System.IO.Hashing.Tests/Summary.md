# System.IO.Hashing.Tests Summary

## Latest Run
- **Date:** 2026-02-01
- **CoreCLR:** Tests run: 1061, Passed: 1061, Failed: 0, Skipped: 0
- **Mono Baseline:** Tests run: 1053, Passed: 1053, Failed: 0, Skipped: 0
- **Status:** ✅ All pass

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.IO.Hashing.Tests`

### Extra in CoreCLR (8 tests)

Tests that run on CoreCLR but were skipped on Mono (likely `[SkipOnMono]` tests):

- System.IO.Hashing.Tests.Crc32Tests.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.Crc64Tests.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash32Tests.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash32Tests_Seeded_007.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash32Tests_Seeded_f00d.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash64Tests.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash64Tests_Seeded_007.AppendingEmptyHasNoEffect
- System.IO.Hashing.Tests.XxHash64Tests_Seeded_f00d.AppendingEmptyHasNoEffect

### Missing in CoreCLR (0 tests)

✅ All Mono tests also ran on CoreCLR.

## Disabled Tests (ActiveIssue #123011)

None.

## Failures and Asserts

None.
