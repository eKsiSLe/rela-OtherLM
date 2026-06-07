# rēlā - Other Launch Monitor Connector API

This folder is prepared to become the external GitHub repository for `Other` launch monitor connectors for `rēlā`.

It documents the connector surface used by `rēlā` and ships a minimal template plugin project so contributors can create and publish their own connector DLLs.

## What this repository contains

- A stable description of the `Other` connector contract.
- A sample implementation that demonstrates the required `ILMDevice` surface.
- Guidance for building, wiring, and validating connectors without reading host internals.

## Architecture in `rēlā` (high level)

`rēlā` discovers external LM plugins from the application directory.

1. `PluginFinder` loads each local `.dll` and resolves public concrete `ILMDevice` implementations.
2. For `Other`, `PluginFinder` selects the first `ILMDevice` that is not a clearly in-process built-in family.
3. `DeviceSessionCoordinator` owns plugin lifecycle calls:
   - `Init()` when the device profile is selected.
   - `Discover()` when user clicks Discover/Search.
   - `Connect()` when a direct connect is requested.
4. The UI subscribes to plugin events and drives transport/state from those events.

## Connector lifecycle contract

The host does not need any special bootstrap from plugins beyond this interface.

1. Implement `ILMDevice` from the `relaDevicePlugin` API.
2. Keep methods idempotent:
   - `Discover()` and `Connect()` should safely return `true` when called more than once.
3. Use status events to communicate state transitions.
4. Emit shot/raw events only after transport and calibration/setup are active.
5. On shutdown, `Disconnect()` should stop activity and clear readiness state.

## ILMDevice contract

`ILMDevice` requires:

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
- `event Action<DeviceShotData> OnBallData`
- `event Action<DeviceShotData> OnShot`
- `event Action<DeviceShotData> OnShotEnded`
- `event Action<DeviceRawShot> OnRawShot`
- `event Action<string> OnNotification`
- `event Action<string> OnHandedChange`
- `event Action<string> OnModeChange`
- `event Action<string> OnError`
- `event Action<string> OnNote`

### Event meanings

- `OnBallData`: live/incremental updates while a shot is building.
- `OnShotEnded`: finalized shot that `rēlā` treats as terminal shot for processing.
- `OnShot`: compatibility alias for final shot; emit the same payload as `OnShotEnded`.
- `OnRawShot`: optional raw payload for optional logging and diagnostics.
- `OnNotification`: status text updates (human-readable).
- `OnHandedChange`: emit `"RH"` or `"LH"` whenever handedness changes.
- `OnModeChange`: emit normalized mode values (`NORMAL`, `PUTTING`, `CHIPPING`).
- `OnError`: non-fatal and recoverable errors only.
- `OnNote`: optional detailed notes/status stream.

### Optional `Other` state extension points

For `Other`, `rēlā` reads these optional members by reflection:

- `bool OtherIsReady`
- `bool OtherBallPresent`

They are not part of `ILMDevice`, but `rēlā` uses them when available.

- `OtherIsReady` affects UI ready/connected indicator and discovery flow.
- `OtherBallPresent` affects UI ball-detection state.

Set these fields as soon as the underlying transport changes state.

## Shot payload contract (important)

`rēlā` does not provide a method like `SendShot()` on the connector API.
The only data path is event emission.

### Required behavior

1. Emit one or more `OnBallData` frames as the shot evolves.
2. Emit one final frame using:
   - `OnShot(finalShot)`
   - `OnShotEnded(finalShot)`
3. Optionally emit raw telemetry with `OnRawShot`.

### Minimal reliable fields

- `IsShotValid` must be accurate for each event.
- At minimum, provide:
  - `Speed`
  - `CarryDistance` (if known)
- Recommended fields when available:
  - `HLA`
  - `VLA`
  - `BackSpin`
  - `SideSpin`
  - `TotalSpin`
  - `ClubSpeed`
  - `Notes`

When a field is unknown, leave it `null` rather than manufacturing values.

### Recommended code shape (pseudo)

```csharp
// Example in your parser/device callback path.
void OnTrackingFrame(TrackerFrame frame)
{
    var shot = new DeviceShotData
    {
        IsShotValid = frame.Valid,
        Speed = frame.SpeedMph,
        HLA = frame.Hla,
        VLA = frame.Vla,
        CarryDistance = frame.CarryYards
    };

    OnBallData(shot); // optional interim updates
}

void OnTrackingFinalFrame(TrackerFrame frame)
{
    var shot = MapFrameToDeviceShotData(frame);
    OnShot(shot);       // compatibility alias
    OnShotEnded(shot);  // primary final-shot event
}
```

The sample file still includes `EmitExampleShot()` to demonstrate event shape, but connectors should wire this method to real hardware callbacks instead of a manual test trigger.

## Template project

Starter plugin files are located in:

- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorDevice.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnectorModule.cs`
- `src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj`

### Build

```bash
dotnet build src/ExampleOtherLmConnector/ExampleOtherLmConnector.csproj
```

## Deployment

1. Build the connector assembly.
2. Copy the output DLL next to `rela.exe`.
3. Open `rēlā`, select `Other` as Device Type.
4. Click Discover, then Connect.
5. Verify output in logs (`OnNotification`, `OnError`, shot events).

## Recommended implementation style

- Keep dependencies small.
- Keep transport and state management inside the plugin assembly.
- Preserve connector stability:
  - avoid throwing exceptions from interface methods,
  - return `false` on failure paths.
- Emit `OnModeChange` and `OnHandedChange` whenever mode/handedness changes.
- Use consistent naming in `GetDeviceName()` for a clear UI identity.

## Open-source onboarding checklist

- [ ] Create your own GitHub repo with this folder as the root.
- [ ] Add your connector under `src/YourConnectorName/`.
- [ ] Reference the `relaDevicePlugin` API source or a maintained package feed.
- [ ] Add a connector-specific README and license.
- [ ] Tag a release that includes a DLL artifact.
