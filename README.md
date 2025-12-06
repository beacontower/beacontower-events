# BeaconTower.Events

A .NET 10 library for Beacon Tower.

## Setup After Creating From Template

### 1. Initialize Git & Push to GitHub

```bash
git init && git branch -m main
dotnet build && dotnet test
git add . && git commit -m "chore: initial project setup"
git tag v0.0.0  # Ensures first release is 0.1.0
gh repo create beacontower/your-repo-name --public --source=. --push --include-all-branches
git push origin v0.0.0
```

### 2. Configure Repository Settings

Go to **Settings → Actions → General**:
- Workflow permissions: **Read and write permissions**
- Check: **Allow GitHub Actions to create and approve pull requests**

### 3. Update Package Metadata

Edit `src/BeaconTower.Events/BeaconTower.Events.csproj`:
```xml
<PackageId>BeaconTower.YourLibrary</PackageId>
<Description>Your library description</Description>
```

Edit `Directory.Build.props`:
```xml
<Product>YourLibrary</Product>
<PackageProjectUrl>https://github.com/beacontower/your-repo</PackageProjectUrl>
<RepositoryUrl>https://github.com/beacontower/your-repo</RepositoryUrl>
<PackageTags>beacontower;your;tags</PackageTags>
```

### 4. First Release

Commit with `feat:` to trigger the first release (0.1.0):
```bash
git add . && git commit -m "feat: initial library implementation"
git push
```

### 5. Delete Example Code (Optional)

Remove the example Calculator class and tests if not needed:
- `src/BeaconTower.Events/Calculator.cs`
- `tests/BeaconTower.Events.Tests/CalculatorTests.cs`

## Development

```bash
dotnet build              # Build
dotnet test               # Run tests
dotnet pack -c Release    # Create NuGet package
```

## CI/CD

- **PR workflow**: Build, test, security scan, pack validation
- **Main workflow**: Semantic release + publish to GitHub Packages

Use conventional commits for automatic versioning:
- `feat:` → minor version bump (0.1.0 → 0.2.0)
- `fix:` → patch version bump (0.1.0 → 0.1.1)
- `feat!:` or `BREAKING CHANGE:` → major version bump (0.1.0 → 1.0.0)

## Consuming the Package

```bash
dotnet add package BeaconTower.Events
```

Requires GitHub Packages source in `nuget.config` (already configured in Beacon Tower projects).
