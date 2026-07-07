# Publishing

NuGet publishing is handled by the manual **Publish to NuGet** GitHub Actions workflow using [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) and [`NuGet/login`](https://github.com/NuGet/login).

## One-Time Setup

Create a Trusted Publishing policy on nuget.org for this repository:

- Repository Owner: `erikbra`
- Repository: `unicode-bidi-net`
- Workflow File: `publish-nuget.yml`
- Environment: leave empty, unless you later add a GitHub Actions environment to the workflow

The workflow file value is the file name only. Do not enter `.github/workflows/publish-nuget.yml`.

NuGet Trusted Publishing exchanges GitHub's OIDC token for a temporary NuGet API key during the workflow run, so no long-lived `NUGET_API_KEY` secret is required.

## Dry Run

Run **Actions** > **Publish to NuGet** from the `main` branch with the default inputs. The workflow builds, tests, runs parity checks, runs full conformance by default, packs `Unicode.Bidi`, and uploads the `.nupkg` and `.snupkg` artifacts without publishing.

## Publish

Run the workflow from `main` with:

- `package_version`: the version to publish, for example `0.3.18-preview`.
- `publish_to_nuget`: `true`.
- `nuget_user`: your nuget.org username/profile name, not your email address.
- `confirm_package_id`: `Unicode.Bidi`.
- `run_full_conformance`: keep `true` unless the full conformance workflow has already passed for the same commit.

The workflow refuses to publish from non-`main` refs, refuses to publish without a NuGet user and explicit package-id confirmation, and uses `--skip-duplicate` so a retry does not fail solely because nuget.org already accepted the package.
