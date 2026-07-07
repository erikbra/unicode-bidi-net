# Publishing

NuGet publishing is handled by the manual **Publish to NuGet** GitHub Actions workflow.

## One-Time Setup

Create a repository secret named `NUGET_API_KEY` containing a nuget.org API key with permission to push `Unicode.Bidi` packages and symbols.

## Dry Run

Run **Actions** > **Publish to NuGet** from the `main` branch with the default inputs. The workflow builds, tests, runs parity checks, runs full conformance by default, packs `Unicode.Bidi`, and uploads the `.nupkg` and `.snupkg` artifacts without publishing.

## Publish

Run the workflow from `main` with:

- `package_version`: the version to publish, for example `0.3.18-preview`.
- `publish_to_nuget`: `true`.
- `confirm_package_id`: `Unicode.Bidi`.
- `run_full_conformance`: keep `true` unless the full conformance workflow has already passed for the same commit.

The workflow refuses to publish from non-`main` refs, refuses to publish without the explicit package-id confirmation, and uses `--skip-duplicate` so a retry does not fail solely because nuget.org already accepted the package.
