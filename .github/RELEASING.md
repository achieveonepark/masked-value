# Releasing Masked Values

The package lives at the repository root. OpenUPM packs the tagged checkout with `npm pack`; `package.json.files` defines the distribution. No custom build step or registry token is required.

## Validate a release

1. Update `package.json.version`, `CHANGELOG.md`, and versioned installation examples in both READMEs and the usage guide.
2. Run the checks with Node.js/npm, PowerShell, and .NET 8 installed:

   ```powershell
   & './Tools~/validate-package.ps1' -Tag v1.0.0
   dotnet run --project 'Tests~/SmokeTests/SmokeTests.csproj' --configuration Release
   & './Tools~/pack.ps1'
   ```

3. In Unity, use **Add package from tarball** to import `Artifacts~/com.achieve.masked-values-1.0.0.tgz`. Import **Class Fields** and check compilation. The automated .NET checks do not run the Unity Editor or IL2CPP.
4. Commit the release changes and create an annotated tag on that commit. The tag must be `v` followed by the exact manifest version:

   ```sh
   git tag -a v1.0.0 -m "Masked Values 1.0.0"
   git push origin main v1.0.0
   ```

   If the local tag already exists, push it without creating it again. The validation workflow also runs on version tags and rejects a manifest/tag mismatch. Published versions must get a new version and tag for subsequent fixes; do not move an existing release tag.

## First OpenUPM submission

Pushing a version tag does not register a new package with OpenUPM.

After the public repository contains the release commit and tag, submit [`.github/openupm/com.achieve.masked-values.yml`](openupm/com.achieve.masked-values.yml) as `data/packages/com.achieve.masked-values.yml` in a pull request to [openupm/openupm](https://github.com/openupm/openupm). Use the title `chore(data): new package com.achieve.masked-values`. Alternatively, fill out the [OpenUPM package add form](https://openupm.com/packages/add/).

The metadata filters tags with the `v` prefix and sets the first published version to `1.0.0`. After the submission is merged and the package is built, verify the version on the [package page](https://openupm.com/packages/com.achieve.masked-values/) and install it in a clean Unity project with:

```sh
openupm add com.achieve.masked-values@1.0.0
```

Subsequent matching tags are discovered automatically. See the [OpenUPM submission requirements](https://openupm.com/docs/adding-upm-package) and [Unity package manifest reference](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-manifestPkg.html).
