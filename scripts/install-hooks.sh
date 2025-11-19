#!/bin/sh

HOOK_DIR=".git/hooks"
SCRIPT_DIR="scripts"

if [ ! -d "$HOOK_DIR" ]; then
    echo "Error: .git/hooks directory not found. Are you in the root of the repository?"
    exit 1
fi

echo "Installing pre-commit hook..."
cp "$SCRIPT_DIR/pre-commit" "$HOOK_DIR/pre-commit"
chmod +x "$HOOK_DIR/pre-commit"

echo "✅  Pre-commit hook installed successfully."
