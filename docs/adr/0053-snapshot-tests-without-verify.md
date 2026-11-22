# ADR-0053: Snapshot tests use checked-in expected files, not Verify

- Status: Accepted
- Date: 2026-10-26

## Context

Architecture.md lists Verify for snapshot tests, and `Directory.Packages.props` pinned `Verify.XunitV3` 33.3.4. Verify releases after 1 September 2026 carry a maintenance fee for some users, and from version 33 a SponsorCheck step fails the build with error SC021 until the consuming project declares one MSBuild property: a GitHub sponsor account, a licensed-until month, an exemption claim (for example `OpenSource` or `SmallRevenue`, each with an end month), or `Verify_SponsorshipLicenseIgnored`, which accepts that the build breaches the package license (https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md). With the package referenced, the reporting test project did not build. Each of those declarations is a legal statement that only the project owner can make, and the lead engineer decided not to use Verify for this work and to remove its pin from `Directory.Packages.props`.

## Decision

No project references Verify; this applies to every test project, including the golden corpus. The reporting test project is the first user of the pattern. Expected output lives as plain checked-in files under `tests/L3Lint.Reporting.Tests/Expected/` (`small.sarif`, `small.xml`, `small.md`), copied to the build output. `ExpectedFiles.cs` normalizes line endings to LF on both sides and compares with Shouldly. Nothing in the tests creates or updates an expected file: they are written by hand from the spec and the test fixture, never copied from writer output. Separate tests assert that writers emit LF only, so the normalization cannot hide a CR LF regression.

## Alternatives considered

- Declaring an exemption or a sponsorship: the owner's decision, not a contributor's.
- `Verify_SponsorshipLicenseIgnored`: rejected; it records a license breach.
- Pinning a Verify release from on or before 1 September 2026: possible, but it changes a central package version and needs its own license review.

## Consequences

The golden corpus work (Test and delivery, "Golden corpus"), which names Verify snapshots, needs the same treatment or an owner decision on Verify licensing. Moving these three files to another snapshot tool later is mechanical.

## Spec IDs affected

T-28, T-29, NFR-001
