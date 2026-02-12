#!/bin/bash
set -e

MODEL="${1:-qwen2.5:14b}"
OLLAMA_URL="${OLLAMA_URL:-http://localhost:11434}"

echo "Ollama Model Setup"
echo "  URL:   $OLLAMA_URL"
echo "  Model: $MODEL"
echo ""

echo -n "Waiting for Ollama to be ready..."
for i in $(seq 1 30); do
    if curl -sf "$OLLAMA_URL/api/tags" > /dev/null 2>&1; then
        echo " OK"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo " FAILED"
        echo "Ollama is not reachable at $OLLAMA_URL after 30 attempts."
        echo "Make sure the container is running: docker compose -f docker-compose.ollama.yml up -d"
        exit 1
    fi
    echo -n "."
    sleep 2
done

echo ""

if curl -sf "$OLLAMA_URL/api/tags" | grep -q "\"$MODEL\""; then
    echo "Model '$MODEL' is already available."
else
    echo "Pulling model '$MODEL'... (this may take several minutes)"
    curl -X POST "$OLLAMA_URL/api/pull" -d "{\"name\":\"$MODEL\"}" --no-buffer | while IFS= read -r line; do
        status=$(echo "$line" | grep -o '"status":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ -n "$status" ]; then
            echo "  $status"
        fi
    done
    echo ""
    echo "Model '$MODEL' is ready."
fi

echo ""
echo "Verify from your dev machine:"
echo "  curl $OLLAMA_URL/api/tags"
echo ""
echo "Run generation:"
echo "  dotnet run -- generate-local-ai-compositions --dry-run"
echo "  dotnet run -- generate-local-ai-compositions --per-subject=2"
