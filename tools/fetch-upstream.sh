#!/usr/bin/env bash
set -euo pipefail

crate_name="${UNICODE_BIDI_CRATE_NAME:-unicode-bidi}"
crate_version="${UNICODE_BIDI_CRATE_VERSION:-0.3.18}"
out_root="${UNICODE_BIDI_UPSTREAM_ROOT:-artifacts/upstream}"
crate_file="${out_root}/${crate_name}-${crate_version}.crate"
extract_dir="${out_root}/${crate_name}-${crate_version}"
url="https://static.crates.io/crates/${crate_name}/${crate_name}-${crate_version}.crate"

mkdir -p "${out_root}"

if [ ! -f "${crate_file}" ]; then
  curl -L "${url}" -o "${crate_file}"
fi

rm -rf "${extract_dir}"
tar -xzf "${crate_file}" -C "${out_root}"

echo "${extract_dir}"
