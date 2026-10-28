# Local verification — 2026-10-28

- All source projects target .NET 10; solution Release build succeeded.
- 9 isolated tests passed, zero failed or skipped.
- Direct and transitive NuGet audit completed with no known advisories reported for the solution and its separate test/preview projects at this snapshot.
- SDK: 10.0.400 on Windows; the local shared runtime is 10.0.11. Source package upgrades do not upgrade that installed runtime. CI selects the current stable .NET 10 SDK/runtime patch; post-publication CI is a separate check.
- Original hosts, database migrations/seeds, payments and cloud consumers were not executed. Build/fixture results do not establish deployment or live integration readiness.

Reproduce using the commands in the README. Audit results depend on the current advisory feed and network access. Existing compiler/nullability warnings may remain. This snapshot is evidence of the stated checks, not a production-readiness certification.

Repository failures caught by the villa and villa-number controllers now return HTTP 500 with a generic APIResponse message. Regression tests inject internal exception text and verify it is absent from the response. These checks cover controller-handled errors; they do not certify every middleware/deployment error path.
