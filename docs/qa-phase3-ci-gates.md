# Phase-3 CI Quality Gates

## Pipeline Overview
- Trigger: push to main/develop, PR to main
- Runner: ubuntu-latest
- .NET version: 8.0.x

## Quality Gates
| Gate | Threshold | Enforced |
|------|-----------|----------|
| Build succeeds | Must pass | Yes |
| All tests pass | 0 failures | Yes |
| Line coverage | >= 35% | Yes |
| Test results published | Artifact | Yes |
| Coverage report published | Artifact | Yes |

## How It Works
1. Restores NuGet packages
2. Builds in Release mode
3. Runs tests with code coverage collection
4. Parses Cobertura XML for line coverage rate
5. Fails if coverage < 35%
6. Publishes test results and coverage as artifacts

## Future Enhancements (Phase-4)
- Add Stryker.NET mutation testing step
- Add complexity analysis step
- Add security scanning (dotnet-security-reporter)
- Add performance benchmark step
- Add Docker build + image scan
