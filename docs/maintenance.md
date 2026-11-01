# Maintenance

## Version policy and compatibility

Starting with package `0.4.0`, the algorithm remains based on Rust `unicode-bidi`
`0.3.18`, while character data follows Unicode `18.0.0` directly. Package versions
advance independently of the Rust crate version. Public signatures, UTF-16 indices,
and the `net10.0` target are unchanged.

Character classifications and resulting visual order can change when Unicode data
changes. In particular, the official UCD assigns `BN` to noncharacters such as
U+FDD0 and U+10FFFF, where Rust `0.3.18` supplied `L`. Regional defaults for
unassigned Arabic code points also use the UCD's `@missing` assignments. These are
intentional differences from the old reference data, not parity exclusions.

The curated Rust parity suite checks shared behavior. Unicode's current data and
full conformance corpus define the character-data contract. Canonically equivalent
brackets, such as U+2329 and U+3008, continue to share an opening bracket identity
under UAX #9 BD16.

## Updating Unicode data

1. Review the new release, UAX #9 changes and errata at
   <https://www.unicode.org/versions/latest/> and <https://www.unicode.org/reports/tr9/>.
2. Update `UnicodeBidiNetUnicodeVersion` in `Directory.Build.props`. It controls the
   downloader, generator, conformance runner and package release notes.
3. Download and regenerate:

   ```sh
   python3 tools/conformance/fetch_unicode_bidi_tests.py --include-character-data
   python3 tools/generate_char_data.py --out src/Unicode.Bidi/char_data/tables.g.cs
   python3 -m unittest discover -s tools -p 'test_*.py' -v
   ```

   The generated header records the version, source URL and SHA-256 of each input.
   Keep `UNICODE-LICENSE.txt` and the third-party notices current.
4. Review the generated diff, update the version assertion and add targeted cases
   for changed character assignments. Build, run the .NET tests and curated Rust
   parity suite, then run the full conformance command in `docs/conformance.md`.
   Change the full baseline's case count only after inspecting the new corpus;
   retain zero permitted failures.
5. Advance the package version in `Directory.Build.props` and the publishing
   workflow's default. Update the README and compatibility/conformance notes.
   Publishing remains a separate manual operation described in `docs/publishing.md`.

The legacy `--tables-rs` generator input is retained to reproduce the Rust 0.3.18
data for comparisons; it is not the source of the shipped current tables.

## Dependency and workflow upkeep

Dependabot checks NuGet, GitHub Actions and both Rust harnesses weekly. Minor and
patch NuGet updates are grouped; major updates remain separate. Rust reference
updates need a review of algorithm changes, source tracing and parity expectations
before merging. Unicode data versions need the manual procedure above.

The test project uses `xunit.v3.mtp-off` 4.x to retain the existing VSTest runner and
coverlet collector. This avoids an unrelated migration to Microsoft Testing Platform.
NuGet restore audits all dependencies and treats vulnerabilities and unavailable
audit feeds (NU1900–NU1905) as errors. The configured audit source is NuGet's public
vulnerability feed.

The full Unicode workflow runs on pull requests, pushes to `main`, weekly, and on
manual dispatch. It also checks that the committed tables reproduce from the UCD
inputs. GitHub can disable scheduled workflows after 60 days of repository
inactivity; if scheduled runs stop, check the workflow's state in Actions and
re-enable it. This repository does not create artificial commits to keep schedules
alive.
