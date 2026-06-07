using System;
using System.Collections.Generic;
using relaDevicePlugin;

namespace ExampleOtherLmConnector;

public sealed class ExampleOtherLmConnectorDevice : ILMDevice
{
    private string _mode = "NORMAL";
    private string _handed = "RH";

    public bool OtherIsReady { get; private set; }
    public bool OtherBallPresent { get; private set; }

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
        OtherIsReady = false;
        OtherBallPresent = false;
        OnNotification.Invoke("[Other Template] Initialized.");
    }

    public string GetDeviceName() => "Example Other Connector";

    public bool Discover()
    {
        // For this template, discover and connect are same path.
        return Connect();
    }

    public bool Connect()
    {
        try
        {
            OtherIsReady = true;
            OnNotification.Invoke("[Other Template] Connected.");
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
        OtherIsReady = false;
        OtherBallPresent = false;
        OnNotification.Invoke("[Other Template] Disconnected.");
        return true;
    }

    public bool SetRightHanded()
    {
        _handed = "RH";
        OnHandedChange.Invoke("RH");
        return true;
    }

    public bool SetLeftHanded()
    {
        _handed = "LH";
        OnHandedChange.Invoke("LH");
        return true;
    }

    public bool SetPuttingMode()
    {
        _mode = "PUTTING";
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool SetChippingMode()
    {
        _mode = "CHIPPING";
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool SetNormalMode()
    {
        _mode = "NORMAL";
        OnModeChange.Invoke(_mode);
        return true;
    }

    public bool ResetReady()
    {
        // Clear ball present state, then mark ready so UI can be re-armed.
        OtherBallPresent = false;
        OtherIsReady = true;
        OnNotification("[Other Template] Ready reset.");
        return true;
    }

    // Sample helper only.
    // rēlā connectors publish data by raising ILMDevice events.
    // This method demonstrates a real payload shape by emitting
    // live (`OnBallData`) + final (`OnShot` + `OnShotEnded`) events.
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

        OnBallData(shot);
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
}
