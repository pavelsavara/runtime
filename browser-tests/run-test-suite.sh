#!/bin/bash

# Script to run a single browser test suite with CoreCLR
# Usage: ./browser-tests/run-test-suite.sh <SuiteName> <csprojPath> [-c <Configuration>] [-m <Method>]
# Example: ./browser-tests/run-test-suite.sh System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj
# Example: ./browser-tests/run-test-suite.sh System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj -c Debug
# Example: ./browser-tests/run-test-suite.sh System.Runtime.InteropServices.JavaScript.Tests src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.Tests/System.Runtime.InteropServices.JavaScript.Tests.csproj -m System.Runtime.InteropServices.JavaScript.Tests.MarshalTests.TestFunctionApply
#
# Follows the process documented in test-suite.md

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

SUITE_NAME="$1"
CSPROJ_PATH="$2"
CONFIG="Release"
METHOD=""

# Parse optional arguments
shift 2 2>/dev/null || true
while getopts "c:m:" opt 2>/dev/null; do
    case $opt in
        c) CONFIG="$OPTARG" ;;
        m) METHOD="$OPTARG" ;;
    esac
done

if [ -z "$SUITE_NAME" ] || [ -z "$CSPROJ_PATH" ]; then
    echo "Usage: $0 <SuiteName> <csprojPath> [-c <Configuration>] [-m <Method>]"
    echo "Example: $0 System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj"
    echo "Example: $0 System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj -c Debug"
    echo "Example: $0 System.Runtime.InteropServices.JavaScript.Tests ... -m System.Runtime.InteropServices.JavaScript.Tests.MarshalTests.TestFunctionApply"
    exit 1
fi

cd "$REPO_ROOT"

# Set environment for CoreCLR Browser/WASM testing
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
export PATH="$REPO_ROOT/.dotnet:$PATH"

# Prepare results directory
RESULTS_DIR="$SCRIPT_DIR/results/$SUITE_NAME"
mkdir -p "$RESULTS_DIR"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BUILD_LOG="$RESULTS_DIR/build_${TIMESTAMP}.log"

echo "=========================================="
echo "Running: $SUITE_NAME"
echo "csproj:  $CSPROJ_PATH"
echo "Config:  $CONFIG"
if [ -n "$METHOD" ]; then
    echo "Method:  $METHOD"
fi
echo "Results: $RESULTS_DIR"
echo "=========================================="

# Download Mono baseline if not exists
"$SCRIPT_DIR/download-mono-baseline.sh" "$SUITE_NAME" || true

# Run tests and capture full output
echo ""
echo "Building and running tests..."
set +e  # Don't exit on test failure

# Build command with optional method filter
BUILD_CMD="dotnet build /t:test \"$CSPROJ_PATH\" -c \"$CONFIG\" -p:TargetOS=browser -p:TargetArchitecture=wasm"
if [ -n "$METHOD" ]; then
    BUILD_CMD="$BUILD_CMD \"/p:WasmTestAppArgs=-method $METHOD\""
fi

eval $BUILD_CMD 2>&1 | tee "$BUILD_LOG"
BUILD_EXIT=$?
set -e

# Extract exit code from xharness output (more reliable than build exit code)
XHARNESS_EXIT=$(grep -oP 'XHarness exit code: \K\d+' "$BUILD_LOG" | tail -1 || echo "")
if [ -z "$XHARNESS_EXIT" ]; then
    XHARNESS_EXIT=$BUILD_EXIT
fi

# Extract test summary
TEST_SUMMARY=$(grep "Tests run:" "$BUILD_LOG" | tail -1 || echo "No test summary found")

# Extract tests run count
TESTS_RUN=$(echo "$TEST_SUMMARY" | grep -oP 'Tests run: \K\d+' || echo "0")

echo ""
echo "XHarness exit code: $XHARNESS_EXIT"
echo "$TEST_SUMMARY"

# Find and copy test results from artifacts
# Paths can vary: net11.0-browser/browser-wasm or net11.0/browser-wasm
RESULTS_COPIED=false
CONSOLE_COPIED=false

for pattern in \
    "$REPO_ROOT/artifacts/bin/$SUITE_NAME/$CONFIG/net*-browser/browser-wasm/wwwroot/xharness-output" \
    "$REPO_ROOT/artifacts/bin/$SUITE_NAME/$CONFIG/net*/browser-wasm/wwwroot/xharness-output"
do
    for dir in $pattern; do
        if [ -d "$dir" ]; then
            # Copy testResults.xml
            if [ -f "$dir/testResults.xml" ] && [ "$RESULTS_COPIED" = false ]; then
                cp "$dir/testResults.xml" "$RESULTS_DIR/testResults_${TIMESTAMP}.xml"
                echo "Copied: testResults_${TIMESTAMP}.xml"
                RESULTS_COPIED=true
            fi
            
            # Copy wasm-console.log (the console output from the test run)
            if [ -f "$dir/wasm-console.log" ] && [ "$CONSOLE_COPIED" = false ]; then
                cp "$dir/wasm-console.log" "$RESULTS_DIR/console_${TIMESTAMP}.log"
                echo "Copied: console_${TIMESTAMP}.log"
                CONSOLE_COPIED=true
            fi
            
            # Also check for browser-wasm.log (alternative name)
            if [ -f "$dir/browser-wasm.log" ] && [ "$CONSOLE_COPIED" = false ]; then
                cp "$dir/browser-wasm.log" "$RESULTS_DIR/console_${TIMESTAMP}.log"
                echo "Copied: console_${TIMESTAMP}.log (from browser-wasm.log)"
                CONSOLE_COPIED=true
            fi
        fi
    done
done

# Report what was copied
echo ""
if [ "$RESULTS_COPIED" = false ]; then
    echo "⚠️  Warning: Could not find testResults.xml"
fi
if [ "$CONSOLE_COPIED" = false ]; then
    echo "⚠️  Warning: Could not find console log (wasm-console.log or browser-wasm.log)"
fi

# Extract individual failures to markdown files
if [ "$RESULTS_COPIED" = true ]; then
    FAILURES_DIR="$RESULTS_DIR/failures_${TIMESTAMP}"
    RESULTS_XML="$RESULTS_DIR/testResults_${TIMESTAMP}.xml"

    # Count failures (handle both NUnit format 'result="Failed"' and xUnit format 'result="Fail"')
    FAILURE_COUNT=$(grep -cE '<test[^>]*result="Fail(ed)?"' "$RESULTS_XML" 2>/dev/null) || FAILURE_COUNT=0

    if [ "$FAILURE_COUNT" -gt 0 ]; then
        mkdir -p "$FAILURES_DIR"
        echo ""
        echo "Extracting $FAILURE_COUNT failure(s) to: $FAILURES_DIR"

        # Use Python to parse XML and extract failures (more reliable than grep/sed)
        export RESULTS_XML FAILURES_DIR TIMESTAMP CSPROJ_PATH CONFIG SCRIPT_DIR
        python3 << 'PYEOF'
import xml.etree.ElementTree as ET
import os
import re
from collections import defaultdict

results_xml = os.environ.get('RESULTS_XML', '')
failures_dir = os.environ.get('FAILURES_DIR', '')
timestamp = os.environ.get('TIMESTAMP', '')

def sanitize_filename(name):
    # Replace invalid filename characters
    return re.sub(r'[<>:"/\\|?*]', '_', name)[:200]

def extract_method_name(full_name):
    # Extract method name without parameters
    # e.g., "Namespace.Class.Method(param1, param2)" -> "Namespace.Class.Method"
    match = re.match(r'^([^(]+)', full_name)
    if match:
        return match.group(1).strip()
    return full_name

def extract_params(full_name):
    # Extract parameters from test name
    # e.g., "Namespace.Class.Method(param1, param2)" -> "(param1, param2)"
    match = re.search(r'(\([^)]*\))$', full_name)
    if match:
        return match.group(1)
    return ""

tree = ET.parse(results_xml)
root = tree.getroot()

# Extract suite name from XML
# xUnit format: <assembly name="...">
# NUnit format: <test-suite type="Assembly" name="...">
suite_name = None
for assembly in root.iter('assembly'):
    suite_name = assembly.get('name', '')
    if suite_name:
        # Remove .dll extension if present
        if suite_name.endswith('.dll'):
            suite_name = suite_name[:-4]
        break
if not suite_name:
    for test_suite in root.iter('test-suite'):
        if test_suite.get('type') == 'Assembly':
            suite_name = test_suite.get('name', '')
            if suite_name:
                break

# Group failures by method name
failures_by_method = defaultdict(list)

# Handle xUnit format: <test result="Fail">
for test_elem in root.iter('test'):
    result = test_elem.get('result', '')
    if result == 'Fail':
        full_name = test_elem.get('name', 'unknown')
        method_name = extract_method_name(full_name)
        params = extract_params(full_name)

        # Extract failure message and stack trace
        failure = test_elem.find('failure')
        message = ""
        stack_trace = ""

        if failure is not None:
            msg_elem = failure.find('message')
            if msg_elem is not None:
                # Handle CDATA content
                message = msg_elem.text or ''

            stack_elem = failure.find('stack-trace')
            if stack_elem is not None:
                # Handle CDATA content
                stack_trace = stack_elem.text or ''

        failures_by_method[method_name].append({
            'params': params,
            'message': message.strip() if message else '',
            'stack_trace': stack_trace.strip() if stack_trace else ''
        })

# Handle NUnit format: <test-case result="Failed">
for test_case in root.iter('test-case'):
    result = test_case.get('result', '')
    if result == 'Failed':
        full_name = test_case.get('fullname') or test_case.get('name', 'unknown')
        method_name = extract_method_name(full_name)
        params = extract_params(full_name)

        # Extract failure message and stack trace
        failure = test_case.find('failure')
        message = ""
        stack_trace = ""

        if failure is not None:
            msg_elem = failure.find('message')
            if msg_elem is not None:
                message = msg_elem.text or ''

            stack_elem = failure.find('stack-trace')
            if stack_elem is not None:
                stack_trace = stack_elem.text or ''

        # Also check for reason element (some formats use this)
        reason = test_case.find('reason')
        if reason is not None:
            reason_msg = reason.find('message')
            if reason_msg is not None and reason_msg.text:
                if not message:
                    message = reason_msg.text

        failures_by_method[method_name].append({
            'params': params,
            'message': message.strip() if message else '',
            'stack_trace': stack_trace.strip() if stack_trace else ''
        })

# Create one markdown file per method
for method_name, failures in failures_by_method.items():
    safe_name = sanitize_filename(method_name)
    filename = f"{safe_name}_{timestamp}.md"
    filepath = os.path.join(failures_dir, filename)

    with open(filepath, 'w') as f:
        f.write(f"# Test Failure: {method_name}\n\n")
        f.write(f"**Timestamp:** {timestamp}\n\n")
        f.write(f"**Full Name:** `{method_name}`\n\n")
        f.write(f"**Failed Cases:** {len(failures)}\n\n")

        for i, fail in enumerate(failures, 1):
            params = fail['params']
            message = fail['message']
            stack_trace = fail['stack_trace']

            f.write(f"---\n\n")
            f.write(f"### Case {i}: `{params}`\n\n")

            if message:
                f.write("**Error Message:**\n\n")
                f.write("```\n")
                f.write(message)
                f.write("\n```\n\n")

            if stack_trace:
                f.write("**Stack Trace:**\n\n")
                f.write("```\n")
                f.write(stack_trace)
                f.write("\n```\n\n")

    print(f"  Created: {filename} ({len(failures)} case(s))")

print(f"Done. {len(failures_by_method)} method(s) with failures extracted.")

# Append self-invocation commands to run-all-failed-tests.sh
script_dir = os.environ.get('SCRIPT_DIR', '')
csproj_path = os.environ.get('CSPROJ_PATH', '')
config = os.environ.get('CONFIG', 'Release')

if script_dir and suite_name and csproj_path and failures_by_method:
    run_all_file = os.path.join(script_dir, 'run-all-failed-tests.sh')

    # Create file with shebang if it doesn't exist
    if not os.path.exists(run_all_file):
        with open(run_all_file, 'w') as f:
            f.write("#!/bin/bash\n")
            f.write("# Auto-generated script to re-run failed tests\n")
            f.write("# Each line runs a single failed test method\n\n")
            f.write("set -e\n\n")
        os.chmod(run_all_file, 0o755)

    # Append commands for each failed method
    with open(run_all_file, 'a') as f:
        for method_name in sorted(failures_by_method.keys()):
            f.write(f'./browser-tests/run-test-suite.sh "{suite_name}" "{csproj_path}" -c {config} -m {method_name}\n')

    print(f"Appended {len(failures_by_method)} command(s) to: {run_all_file}")
PYEOF
    fi
fi

# Run comparison if Mono baseline exists
if [ -f "$RESULTS_DIR/mono-testResults.xml" ] && [ "$RESULTS_COPIED" = true ]; then
    echo ""
    echo "Running comparison..."
    "$SCRIPT_DIR/compare-test-results.sh" "$SUITE_NAME" 2>&1 | tail -20
fi

# Output final status
echo ""
echo "=========================================="
if [ -n "$METHOD" ] && [ "$TESTS_RUN" = "0" ]; then
    echo "⚠️  $SUITE_NAME: NO TESTS RAN"
    echo "=========================================="
    echo ""
    echo "Warning: Method filter '-m $METHOD' did not match any tests."
    echo "Check that the method name is fully qualified (Namespace.Class.Method)"
    exit 1
elif [ "$XHARNESS_EXIT" = "0" ]; then
    echo "✅ $SUITE_NAME: PASSED"
    echo "=========================================="
else
    echo "❌ $SUITE_NAME: FAILED (exit code: $XHARNESS_EXIT)"
    echo "=========================================="
    echo ""
    echo "Next steps (per test-suite.md):"
    echo "  1. Check console log: $RESULTS_DIR/console_${TIMESTAMP}.log"
    echo "  2. Check test results: $RESULTS_DIR/testResults_${TIMESTAMP}.xml"
    echo "  3. For failures: Mark with [ActiveIssue(\"https://github.com/dotnet/runtime/issues/123011\")]"
    echo "  4. For timeouts: Find last running test in console log"
fi

exit ${XHARNESS_EXIT:-1}
