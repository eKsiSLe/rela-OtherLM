# rēlā - Other Launch Monitor Connector API

This repository documents and demonstrates the external `Other` launch monitor connector API for `rēlā`.

It documents the contract already implemented in the application and ships a minimal template plugin project so contributors can create and publish connector DLLs.

## What the repo is for

- Provide a stable source-of-truth for the `Other` plugin surface.
- Show how `rēlā` discovers and runs third-party launch monitor connectors.
- Offer a minimal starter plugin so people can begin integration without needing to reverse engineer the host project.

## Current architecture in `rēlā`

`rēlā` already loads external plugins from the application directory using type discovery:

1. User selects `Other` in settings (which is intentionally routed through the same external backend).
2. `PluginFinder` scans each local `.dll`, loads assemblies, and registers all public non-abstract types implementing `ILMDevice`.
3. For `Other`, `PluginFinder` picks the first registered `ILMDevice` that is not clearly a built-in family.
4. `DeviceSessionCoordinator` calls:
   - `Init()` when the device instance is selected.
   - `Discover()` when the user clicks Discover/Search.
   - `Connect()` when a direct connect is requested.
5. The UI consumes shot and state events and passes shot data into telemetry and simulator transport.

## Contract to implement

Connector implementations must implement `ILMDevice` from the `rela.OtherDevice.Abstractions` NuGet package. The package contains compile-time metadata only; rēlā supplies the matching runtime assembly from its embedded application bundle.

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

### Optional device settings

Connectors that expose their own settings UI can additionally implement `IDeviceSettingsProvider`:

```csharp
public interface IDeviceSettingsProvider
{
    void ShowDeviceSettings();
}
```

When `Other` is selected and the active connector implements this interface, rēlā shows a **Device Settings** button in the Settings window. Clicking it calls `ShowDeviceSettings()` on the active connector.

The connector owns the complete settings experience: UI, validation, persistence, and applying changes to a live device. The host does not inspect or render connector setting fields. `ShowDeviceSettings()` must marshal to the appropriate UI thread when needed and should report failures through the normal plugin events rather than throwing into the host.

The example connector implements this complete path. Its plugin-owned dialog edits handedness and mode, persists them to `Settings/Other/example-other-connector.json`, and publishes `OnHandedChange` and `OnModeChange` after a successful save. See `ExampleOtherLmConnectorDevice.ShowDeviceSettings()` and `ExampleOtherLmConnectorSettingsDialog`.

## Shot calls and payload expectations

`rēlā` consumes `DeviceShotData` and optionally `DeviceRawShot`.

`rēlā` receives shot information through callback-style events; there are four calls that matter to plugin authors.

1. `OnBallData(DeviceShotData shot)`
   - Use this for **in-flight** telemetry and pre-impact updates.
   - `rēlā` treats these as `ShotPhase.BallData` and may use them for streaming telemetry/ready-state logic.
   - This can be called 0..N times per shot window.

2. `OnShot(DeviceShotData shot)`
   - This event is part of the API surface for compatibility with older integrations.
   - In current `rēlā` flow it is not wired to transport, so emit it only if you need a dedicated separator event for tooling.
   - The preferred final-shot path for `rēlā` is `OnShotEnded`.

3. `OnShotEnded(DeviceShotData shot)`
   - This is the primary final-shot event for `rēlā`.
   - When emitted, `rēlā` marks the payload as `ShotPhase.ShotEnded` and routes it through normal shot dispatch.
   - Emit this once per shot once all metrics are ready and values are final.

4. `OnRawShot(DeviceRawShot rawShot)`
   - Optional telemetry path for raw payload logging.
   - `rēlā` only persists/logs this when raw-shot logging is enabled in settings.

Per-shot payload guidance:
- Set `IsShotValid` accurately. Invalid shots are still accepted for telemetry, but dispatch behavior can be filtered by plugin/device policy.
- Fill core ball metrics when available: `Speed`, `HLA`, `VLA`, `BackSpin`, `SideSpin`, `SpinAxis`, `TotalSpin`, and `CarryDistance`.
- Keep `Speed`/spin/angles in the same unit system you intend to model consistently; `rēlā` applies the same interpretation consistently across plugins.
- Optional `Notes` entries are accepted and can be used for diagnostics.
- Include optional club metrics (`ClubSpeed`, angles, impact fields, confidence fields) when available so downstream inference/quality logic can use them.

## Template project

A starter plugin with the full event/method surface is under:

- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorDevice.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorSettings.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorSettingsDialog.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj`

### Build

Restore and build the example normally. The API contract is restored from NuGet.org:

```bash
dotnet build src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj
```

## Deploy

1. Build the connector assembly.
2. Copy only the connector DLL next to `rela.exe`. Do not deploy files from `rela.OtherDevice.Abstractions`; rēlā supplies that contract at runtime.
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

- [ ] Create a repo with this folder as the root.
- [ ] Add your connector in `src/YourConnectorName/`.
- [ ] Reference a `rela.OtherDevice.Abstractions` package version supported by the target rēlā release.
- [ ] Add a connector-specific README and license.
- [ ] Tag a release with a DLL artifact for users.
