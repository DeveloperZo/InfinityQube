#!/usr/bin/env bash
# Analytics helper for InfinityQube build health
# Usage: ./shrimpScripts/analytics.sh [command]

COMMANDS="health|failures|timing|streak"

show_help() {
    echo "InfinityQube Build Analytics"
    echo "Usage: $0 [${COMMANDS}]"
    echo ""
    echo "Commands:"
    echo "  health    - Show current build health status"
    echo "  failures  - Show recent build failures"
    echo "  timing    - Show build/test performance trends"
    echo "  streak    - Show healthy build streak info"
    echo ""
}

show_health() {
    echo "=== BUILD HEALTH STATUS ==="
    echo "🟢 Recent successful builds:"
    shrimp query --field build_ok=true --limit 5 --format table
    echo ""
    echo "🔴 Recent failures:"
    shrimp query --field build_ok=false --limit 3 --format table
}

show_failures() {
    echo "=== RECENT BUILD FAILURES ==="
    shrimp query --field build_ok=false --limit 10 --format json | \
        jq -r '.[] | "❌ \(.id[0:8]) [\(.last_red_at // "unknown")] \(.failure_reason // "No reason logged")"'
}

show_timing() {
    echo "=== BUILD PERFORMANCE TRENDS ==="
    echo "Last 10 successful builds:"
    shrimp query --field build_ok=true --limit 10 --format json | \
        jq -r '.[] | "⏱️  Build: \(.build_time_ms // 0)ms | Tests: \(.test_time_ms // 0)ms | \(.id[0:8])"' | \
        sort -n
}

show_streak() {
    echo "=== HEALTHY BUILD STREAK ==="
    LAST_RED=$(shrimp query --field build_ok=false --limit 1 --format json | jq -r '.[0].last_red_at // "Never"')
    GREEN_COUNT=$(shrimp query --field build_ok=true --limit 100 --format json | jq 'length')
    
    echo "🏆 Consecutive green builds: $GREEN_COUNT"
    echo "🔴 Last red build: $LAST_RED"
    
    if [ "$GREEN_COUNT" -gt 20 ]; then
        echo "🎉 Excellent build health!"
    elif [ "$GREEN_COUNT" -gt 10 ]; then
        echo "✅ Good build stability"
    elif [ "$GREEN_COUNT" -gt 5 ]; then
        echo "⚠️  Moderate build health"
    else
        echo "🚨 Build health needs attention"
    fi
}

# Main command handling
case "${1:-help}" in
    health)   show_health ;;
    failures) show_failures ;;
    timing)   show_timing ;;
    streak)   show_streak ;;
    help|*)   show_help ;;
esac
