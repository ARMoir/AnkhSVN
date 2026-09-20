# ARMoir fork maintenance

This directory is intentionally stored only on the `fork-maintenance` branch of `ARMoir/AnkhSVN`.

**Do not merge or cherry-pick these files into `main`, and do not include them in an upstream AmpScm/AnkhSVN pull request.**

## v2.9 cleanup

`cleanup-v29.ps1` removes historical `v2.9.xxx` GitHub objects while preserving:

- the newest `v2.9.xxx` Release;
- the newest `AnkhSVN.VSIX.2022` 2.9.x package version;
- the newest deployment for each deployment environment;
- all non-v2.9 legacy Releases such as `VS20026` and `AnkhSVN`.

The script defaults to a dry run:

    ./fork-maintenance/cleanup-v29.ps1

Use `-Execute` only after reviewing the proposed deletions:

    ./fork-maintenance/cleanup-v29.ps1 -Execute

`cleanup-v29.workflow.yml.template` is deliberately outside `.github/workflows`, so GitHub Actions will not run it automatically.

When remote cleanup is needed, create a temporary `cleanup-v29` branch from `fork-maintenance`, copy the template to `.github/workflows/cleanup-v29.yml`, let it run once, and delete that temporary branch afterward.

The permanent `fork-maintenance` branch remains separate from `main`, so a normal PR from `ARMoir/AnkhSVN:main` to `AmpScm/AnkhSVN:main` cannot include these maintenance-only files.