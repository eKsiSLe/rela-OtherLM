# rēlā Other Launch Monitor Connector API

This folder is prepared to become the external GitHub repository for `Other` launch monitor connectors for `rēlā`.

It documents the contract already implemented in the application and ships a minimal template plugin project so contributors can create and publish connector DLLs.

## What the repo is for

- Provide a stable source-of-truth for the `Other` plugin surface.
- Show how `rēlā` discovers and runs third-party launch monitor connectors.
- Offer a minimal starter plugin so people can begin integration without needing to reverse engineer the host project.

## Current architecture in `rēlā`

`rēlā` already loads external plugins from the application directory using Autofac discovery:

1. User selects `Other` in settings (which is intentionally routed through the same external backend).
2. `PluginFinder` scans each local `.dll`, loads assemblies, and registers all public non-abstract types implementing `ILMDevice`.
3. For `Other`, `PluginFinder` picks the first registered `ILMDevice` that is not clearly a built-in family.
4. `DeviceSessionCoordinator` calls:
   - `Init()` when the device instance is selected.
   - `Discover()` when the user clicks Discover/Search.
   - `Connect()` when a direct connect is requested.
5. The UI consumes shot and state events and passes shot data into telemetry and simulator transport.

## Contract to implement

Connector implementations must implement `ILMDevice` from the `relaDevicePlugin` API package/assembly.

## Interface surface

`ILMDevice` requires:

- Events:
  - `Action<DeviceShotData> OnBallData`
  - `Action<DeviceShotData> OnShot`
  - `Action<DeviceShotData> OnShotEnded`
  - `Action<DeviceRawShot> OnRawShot`
  - `Action<string> OnNotification`
  - `Action<string> OnHandedChange`
  - `Action<string> OnModeChange`
  - `Action<string> OnError`
  - `Action<string> OnNote`
- Methods:
  - `bool SetRightHanded()`
  - `bool SetLeftHanded()`
  - `bool Connect()`
  - `bool Reconnect()`
  - `bool Disconnect()`
  - `bool SetPuttingMode()`
  - `bool SetChippingMode()`
  - `bool SetNormalMode()`
  - `void Init()`
  - `string GetDeviceName()`
  - `bool Discover()`
  - `bool ResetReady()`

For `Other`, the app looks for these optional properties by reflection and falls back to defaults when absent:

- `bool OtherIsReady`
- `bool OtherBallPresent`

These are not part of `ILMDevice` today, but `rēlā` reads them as extension points. Set them as soon as state is known:

- `OtherIsReady` controls the ready indicator.
- `OtherBallPresent` controls the ball detected indicator and readiness gating behavior.

## Shot payload expectations

`rēlā` consumes `DeviceShotData` and optionally `DeviceRawShot`.

- Use `ShotPhase.BallData` for incremental updates and `ShotPhase.ShotEnded` for final shots.
- Use `DeviceShotData` first when `IsShotValid` is known; invalid shots should still be emitted only if your telemetry path requires them.
- For club-related fields, keep values consistent with what your hardware provides.
- `DeviceRawShot` is optional and only used for raw telemetry logging.

## Template project

A starter plugin with the full event/method surface is under:

- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorDevice.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorModule.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj`

### Build

From this workspace:

```bash
dotnet build src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj
```

## Deploy

1. Build the connector assembly.
2. Copy the output DLL next to `rela.exe`.
3. Open `rēlā` and set Device Type to `Other`.
4. Click Discover and then Connect.
5. Use logs to confirm `OnNotification` messages and status updates.

## Repository conventions

- Keep dependencies small.
- Keep plugin state and networking inside the plugin assembly.
- Emit status strings on `OnNotification` and non-fatal detail on `OnError`.
- Use `Other` + optional state properties above for richer UI behavior.
- Prefer stable device names in `GetDeviceName()` so users can identify the plugin quickly.

## Recommended minimum behavior

- Implement `Discover()` and `Connect()` idempotently.
- Return `true` once your plugin has started or scheduled async startup work.
- Set `OtherIsReady` to `true` once the transport/hardware is connected.
- Clear ready/ball state on `Disconnect()` and `Reconnect()`.
- Emit `OnModeChange(...)` and `OnHandedChange(...)` whenever mode/handedness changes so `rēlā` UI stays in sync.
- Avoid throwing out of interface methods; return `false` on failure.

## Open-source onboarding checklist

- [ ] Create a GitHub repo with this folder as the root.
- [ ] Add your connector in `src/YourConnectorName/`.
- [ ] Reference the `relaDevicePlugin` API source in your repository (or consume it from a supported package feed if you maintain one).
- [ ] Add a connector-specific README and license.
- [ ] Tag a release with a DLL artifact for users.
