#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
rust_root="${UNICODE_BIDI_LOCAL_RUST_ROOT:-${root}/artifacts/rust}"
rustup_init="${rust_root}/rustup-init"

mkdir -p "${rust_root}"

if [ ! -x "${rustup_init}" ]; then
  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs -o "${rustup_init}"
  chmod +x "${rustup_init}"
fi

CARGO_HOME="${rust_root}/cargo" \
RUSTUP_HOME="${rust_root}/rustup" \
"${rustup_init}" -y --no-modify-path --profile minimal --default-toolchain stable

echo "Rust installed under ${rust_root}"
echo "Run parity with:"
echo "python3 tools/parity/run_bidi_parity.py --out-dir artifacts/bidi-parity"
