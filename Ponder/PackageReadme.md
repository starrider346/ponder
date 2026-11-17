# Ponder for StellarDrive

Animated part guides for **StellarDrive**, inspired by Create's Ponder system.
The `StellarDrivePonder` NuGet package provides the C# authoring API and
IntelliSense documentation.

Players hold **Z** for one second over a supported part in the build menu to open
its guide. The viewer supports playback, pause, replay, timeline seeking and
camera controls. Bundled guides cover Constant, Fluid Tank, Chemical Thruster,
Basic Pilot Seat and Advanced Controls.

## Add the authoring API

Use the latest stable SSLib and Ponder packages in your guide adapter project:

```xml
<ItemGroup>
  <PackageReference Include="StellarStriderLib" Version="*" ExcludeAssets="runtime" />
  <PackageReference Include="StellarDrivePonder" Version="*" ExcludeAssets="runtime" />
</ItemGroup>
```

The package ID is `StellarDrivePonder`; its assembly name and C# namespace are
`Ponder`. Target `netstandard2.1` and reference the game's Unity and MelonLoader
assemblies separately with `Private=false`. These assemblies are not bundled
with the package.

`ExcludeAssets="runtime"` supplies compile references without copying the mod
DLLs into your output. Players install the latest SSLib and Ponder runtime mods
separately in the game's `Mods` folder. NuGet does not install runtime mods into
the game.

## Keep Ponder optional

Put guide registration and the Ponder package reference in a separate adapter
assembly. Embed that adapter in your main mod and load it only when Ponder is
present. Declare the loader dependencies in your main mod:

```csharp
using MelonLoader;

[assembly: MelonAdditionalDependencies("StellarStriderLib")]
[assembly: MelonOptionalDependencies("Ponder")]
```

Keep the main assembly free of direct Ponder references and leave `ponder` out
of its manifest's required `needs` list. The mod's normal gameplay remains
available when Ponder is absent.

## Author a guide

Register with `PonderRegistry.Create(modId, ownerAssembly).RegisterGuide(...)`
on Unity's main thread during initialization, after registering your SSLib parts.
A `GuideDefinition` contains a title, timed steps, a `BuildScene` callback and
optional custom `Sample` callback. Its `Parts` array attaches the guide to stable
SSLib content keys such as `example.mod:machine`.

Use `PonderScene` to create preview objects, animate movement and rotation, add
highlights and callouts, and frame the camera. Sample custom visual state from
absolute guide time so replay and backward seeking reconstruct the same scene.
Ponder previews are visual only and do not create working parts or save data.

## Licence

MIT. The package includes the repository's `LICENSE` file.
