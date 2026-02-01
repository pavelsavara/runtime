# System.Collections.Concurrent.Tests - Summary

## Test Results

| Metric | CoreCLR | Mono Baseline |
|--------|---------|---------------|
| Tests Run | 2317 | 2316 |
| Passed | 2229 | 2229 |
| Failed | 1 → 0 (marked) | 0 |
| Skipped | 87 → 88 | 87 |

## Status: ⚠️ Has ActiveIssue

One test failure marked with ActiveIssue.

## Test Set Comparison

### Extra in CoreCLR (1 test)
- `System.Collections.Concurrent.Tests.ConcurrentQueueTests.ReferenceTypes_NulledAfterDequeue` - Failed (now marked with ActiveIssue)

This test was already skipped on Mono due to finalizer issues (mono/mono#16413).

### Missing in CoreCLR
None - all Mono tests also ran on CoreCLR.

## Failures

| Test | Type | Link |
|------|------|------|
| ReferenceTypes_NulledAfterDequeue | finalizer | [failure record](../../failures/System.Collections.Concurrent.Tests/ConcurrentQueueTests.ReferenceTypes_NulledAfterDequeue.md) |

## Notes

- Platform: Browser/WASM + CoreCLR
- Configuration: Release
- Date: 2026-02-01
- The test failure is due to finalizers not working on CoreCLR+Browser/WASM (known limitation)
