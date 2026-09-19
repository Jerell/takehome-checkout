#!/bin/sh
# Fire sample payments at the gateway so the metrics have traffic.
# Run from anywhere; once, or repeatedly while watching Grafana.
#
# Usage: ./demo-payments.sh [rounds]   (default: 1 round)
#
# Each round sends one authorized (odd), one declined (even) and one
# rejected (card ending in 0) payment. Override the API with PAYMENT_API_URL.
set -e

BASE="${PAYMENT_API_URL:-http://localhost:5067}"
ROUNDS="${1:-1}"

round=1
while [ "$round" -le "$ROUNDS" ]; do
    echo "--- round $round of $ROUNDS ---"
    n=1
    while [ "$n" -le 3 ]; do
        case "$n" in
            1) card="4000000000000001"; label="authorized" ;;
            2) card="4000000000000002"; label="declined" ;;
            3) card="4000000000000000"; label="rejected" ;;
        esac

        body=$(mktemp)
        code=$(curl -sSLk -o "$body" -w "%{http_code}" -X POST "$BASE/api/payments" \
            -H "Content-Type: application/json" \
            -d "{\"cardNumber\":\"$card\",\"expiryMonth\":12,\"expiryYear\":2029,\"currency\":\"GBP\",\"amount\":1000,\"cvv\":\"123\"}")
        id=$(grep -oE '"id":"[0-9a-f-]{36}"' "$body" | head -1 | cut -d'"' -f4)
        rm -f "$body"

        printf '%-24s http=%-3s id=%s\n' "$label" "$code" "$id"
        n=$((n + 1))
    done
    round=$((round + 1))
done
echo "done. Watch the Payments rate panel spike."