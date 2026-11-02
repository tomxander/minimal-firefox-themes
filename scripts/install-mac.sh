#!/usr/bin/env bash
# Install one theme into a Firefox profile on macOS (or Linux).
#   bash scripts/install-mac.sh minimal
#   bash scripts/install-mac.sh luna-2026 --profile "$HOME/Library/Application Support/Firefox/Profiles/abcd1234.default-release"
set -euo pipefail

theme="${1:-}"
profile=""
if [[ "${2:-}" == "--profile" ]]; then profile="${3:-}"; fi

repo="$(cd "$(dirname "$0")/.." && pwd)"
src="$repo/themes/$theme"
if [[ -z "$theme" || ! -d "$src" ]]; then
  echo "Usage: $0 <theme> [--profile <path>]"
  echo "Themes:"; ls "$repo/themes"
  exit 1
fi

if [[ -z "$profile" ]]; then
  case "$(uname)" in
    Darwin) base="$HOME/Library/Application Support/Firefox/Profiles" ;;
    *)      base="$HOME/.mozilla/firefox" ;;
  esac
  profile="$(ls -d "$base"/*.default-release 2>/dev/null | head -1 || true)"
  [[ -z "$profile" ]] && profile="$(ls -d "$base"/*.default 2>/dev/null | head -1 || true)"
fi
if [[ -z "$profile" || ! -d "$profile" ]]; then
  echo "No Firefox profile found. Pass one with --profile <path> (see about:profiles in Firefox)."
  exit 1
fi

if pgrep -x firefox >/dev/null 2>&1 || pgrep -f "Firefox.app" >/dev/null 2>&1; then
  echo "Firefox is running. Quit it first (Cmd+Q), then run this again."
  exit 1
fi

mkdir -p "$profile/chrome"
cp "$src/userChrome.css" "$profile/chrome/userChrome.css"
cp "$src/userContent.css" "$profile/chrome/userContent.css"
cp "$src/user.js" "$profile/user.js"

# The themes that show the menu bar (Windows-only concept; harmless on a Mac).
echo "Installed '$theme' into:"
echo "  $profile"
echo "Start Firefox to see it."
