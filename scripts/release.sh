#!/usr/bin/env bash
#
# Fluent Pass - Release Helper Script (Bash)
#
# Usage:
#   ./scripts/release.sh <version> [--push] [--dry-run] [--remote <origin>]
#
# Examples:
#   ./scripts/release.sh v1.0.0 --dry-run
#   ./scripts/release.sh v1.0.0 --push
#

set -euo pipefail

VERSION=""
PUSH=false
DRY_RUN=false
REMOTE="origin"

while [[ $# -gt 0 ]]; do
  case $1 in
    --push)
      PUSH=true
      shift
      ;;
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    --remote)
      REMOTE="$2"
      shift 2
      ;;
    -*)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
    *)
      if [[ -z "$VERSION" ]]; then
        VERSION="$1"
      else
        echo "Unexpected argument: $1" >&2
        exit 1
      fi
      shift
      ;;
  esac
done

if [[ -z "$VERSION" ]]; then
  echo "Error: Version argument is required (e.g. v1.0.0 or 1.0.0)" >&2
  exit 1
fi

if [[ "$VERSION" != v* ]]; then
  VERSION="v$VERSION"
fi

SEMVER_REGEX='^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?(\+([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$'
if [[ ! "$VERSION" =~ $SEMVER_REGEX ]]; then
  echo "Error: Invalid version format '$VERSION'. Must follow SemVer (e.g. v1.0.0, v1.0.0-rc1)." >&2
  exit 1
fi

echo "================================================="
echo "   Fluent Pass - Release Helper Script          "
echo "================================================="
echo "[1/4] Target Version: $VERSION"

GIT_STATUS=$(git status --porcelain)
if [[ -n "$GIT_STATUS" ]]; then
  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[DryRun] Working tree is dirty. (Ignored in dry-run mode):"
    echo "$GIT_STATUS"
  else
    echo "Error: Working tree is dirty! Please commit or stash changes before releasing." >&2
    echo "$GIT_STATUS" >&2
    exit 1
  fi
else
  echo "[2/4] Git working tree is clean."
fi

if git tag -l "$VERSION" | grep -q "^$VERSION$"; then
  echo "Error: Git tag '$VERSION' already exists locally!" >&2
  exit 1
fi
echo "[3/4] Tag '$VERSION' is available."

if [[ "$DRY_RUN" == "true" ]]; then
  echo "-------------------------------------------------"
  echo "[DryRun] All pre-release checks passed!"
  echo "  - Tag: $VERSION"
  echo "  - Remote: $REMOTE"
  echo "  - Will Push: $PUSH"
  echo "To release for real, run:"
  echo "  ./scripts/release.sh $VERSION --push"
  echo "-------------------------------------------------"
  exit 0
fi

echo "Creating annotated Git tag '$VERSION'..."
git tag -a "$VERSION" -m "Release $VERSION"
echo "Successfully created tag '$VERSION'."

if [[ "$PUSH" == "true" ]]; then
  echo "Pushing tag '$VERSION' to remote '$REMOTE'..."
  git push "$REMOTE" "$VERSION"
  echo "-------------------------------------------------"
  echo " Tag '$VERSION' pushed to $REMOTE successfully!"
  echo " GitHub Actions Release Pipeline is now running."
  echo "-------------------------------------------------"
else
  echo "-------------------------------------------------"
  echo "Tag created locally. Push when ready:"
  echo "  git push $REMOTE $VERSION"
  echo "-------------------------------------------------"
fi
