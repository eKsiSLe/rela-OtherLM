using System;
using System.Collections.Generic;
using System.Windows;
using relaDevicePlugin;

namespace ExampleOtherLmConnector;

public sealed class ExampleOtherLmConnectorDevice : ILMDevice, IDeviceSettingsProvider
{
    private ExampleOtherLmConnectorSettings _settings = new();
    private string _mode = "NORMAL";
    private string _handed = "RH";
    private bool _connected;

    public bool SessionConnected => _connected;
    public bool OtherIsReady { get; private set; }
    public bool OtherBallPresent { get; private set; }
    public bool OtherIsArmed { get; private set; }
    // This example emits one final payload through OnShotEnded in every mode.
    public bool UsesTelemetryFinalShotPath => true;
    public string SupportedConnectionTypes => "Direct";

    public event Action<DeviceShotData> OnBallData = delegate { };
    public event Action<DeviceShotData> OnShotEnded = delegate { };
    public event Action<DeviceShotData> OnShot = delegate { };
    public event Action<DeviceRawShot> OnRawShot = delegate { };
    public event Action<string> OnNotification = delegate { };
    public event Action<string> OnHandedChange = delegate { };
    public event Action<string> OnModeChange = delegate { };
    public event Action<string> OnError = delegate { };
    public event Action<string> OnNote = delegate { };

    public void Init()
    {
        try
        {
            _settings = ExampleOtherLmConnectorSettings.Load();
            _mode = _settings.Mode;
            _handed = _settings.Handedness;
        }
        catch (Exception ex)
        {
            OnError.Invoke("[Other Template] Settings load failed: " + ex.Message);
        }

        OtherIsReady = false;
        OtherBallPresent = false;
        OtherIsArmed = false;
        _connected = false;
        OnNotification.Invoke("[Other Template] Initialized.");
    }

    public string GetDeviceName() => "Example Other Connector";

    public void ShowDeviceSettings()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(ShowDeviceSettings);
            return;
        }

        try
        {
            var candidate = _settings.Copy();
            if (!ExampleOtherLmConnectorSettingsDialog.TryCollect(candidate))
                return;

            candidate.Save();
            _settings = candidate;
            _mode = candidate.Mode;
            _handed = candidate.Handedness;
            OnModeChange.Invoke(_mode);
            OnHandedChange.Invoke(_handed);
            OnNotification.Invoke("[Other Template] Device settings saved.");
        }
        catch (Exception ex)
        {
            OnError.Invoke("[Other Template] Settings failed: " + ex.Message);
        }
    }

    public bool Discover()
    {
        // For this template, discover and connect are same path.
        return Connect();
    }

    public bool Connect()
    {
        try
        {
            _connected = true;
            OtherIsReady = true;
            OtherIsArmed = true;
            OnNote.Invoke("[Other] BallStatus: ready=true ball=false");
            OnNotification.Invoke("[Other] Connected: Example Other Connector.");
            OnModeChange.Invoke(_mode);
            OnHandedChange.Invoke(_handed);
            return true;
        }
        catch (Exception ex)
        {
            OnError.Invoke("[Other Template] Connect failed: " + ex.Message);
            return false;
        }
    }

    public bool Reconnect()
    {
        Disconnect();
        return Connect();
    }

    public bool Disconnect()
    {
        _connected = false;
        OtherIsReady = false;
        OtherBallPresent = false;
        OtherIsArmed = false;
        OnNote.Invoke("[Other] BallStatus: ready=false ball=false");
        OnNotification.Invoke("[Other] Disconnected: Example Other Connector.");
        return true;
    }

    public bool SetRightHanded()
    {
        _handed = "RH";
        SaveCurrentSettings();
        OnHandedChange.Invoke("RH");
        return true;
    }

    public bool SetLeftHanded()
    {
        _handed = "LH";
        SaveCurrentSettings();
        OnHandedChange.Invoke("LH");
        return true;
    }

    public bool SetPuttingMode()
    {
        _mode = "PUTTING";
        SaveCurrentSettings();
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool SetChippingMode()
    {
        _mode = "CHIPPING";
        SaveCurrentSettings();
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool SetNormalMode()
    {
        _mode = "NORMAL";
        SaveCurrentSettings();
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool ResetReady()
    {
        // Clear ball present state, then mark ready so UI can be re-armed.
        OtherBallPresent = false;
        OtherIsReady = true;
        OtherIsArmed = true;
        OnNote.Invoke("[Other] BallStatus: ready=true ball=false");
        OnNotification("[Other Template] Ready reset.");
        return true;
    }

    public bool ArmOnly() => ResetReady();

    public void EmitExampleShot()
    {
        var shot = new DeviceShotData
        {
            Speed = 105.0m,
            HLA = 1.2m,
            VLA = 10.5m,
            BackSpin = 3200.0m,
            SideSpin = 4.0m,
            SpinAxis = 7.2m,
            TotalSpin = 3204.0m,
            CarryDistance = 165.0m,
            IsShotValid = true,
            Notes = new List<string> { "example shot" }
        };

        OnShot(shot);
        OnShotEnded(shot);

        OnRawShot(new DeviceRawShot
        {
            InsertedAt = DateTime.UtcNow,
            TotalSpeedMPH = 105.0m,
            TotalSpin = 3200.0m,
            Carry = 165.0m
        });

        OnNotification("[Other Template] Example shot emitted.");
    }

    private void SaveCurrentSettings()
    {
        try
        {
            _settings.Mode = _mode;
            _settings.Handedness = _handed;
            _settings.Save();
        }
        catch (Exception ex)
        {
            OnError.Invoke("[Other Template] Settings save failed: " + ex.Message);
        }
    }
}
