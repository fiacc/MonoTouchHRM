using MonoTouch.CoreBluetooth;
using MonoTouch.CoreFoundation;
using MonoTouch.Foundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


public class HeartRateMonitor
{
    public class HeartRateEventArgs : EventArgs
    {
        public int HeartRate { get; set; }
    }
    /// <summary>
    ///  Constants for HRM
    /// http://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicsHome.aspx
    /// </summary>
    /// </summary>
    private class BluetoothConstants
    {
        public const string HEART_RATE_SERVICE = "180D";
        public const string HEART_RATE_MEASUREMENT = "2A37";
        public const string BODY_SENSOR_LOCATION = "2A38";
        public const string HEART_RATE_CONTROL_POINT = "2A39";
    }
    public delegate void UpdateEventHandler(object sender, HeartRateEventArgs e);
    public event UpdateEventHandler HeartRateUpdated;
    protected virtual void OnHeartRateUpdated(HeartRateEventArgs e)
    {
        if (HeartRateUpdated != null)
        {
            HeartRateUpdated(this, e);
        }
    }

    CBCentralManager _manager;
    CBPeripheral _hrm;
    EmCBCentralManagerDelegate _CBCentralManagerDelegate;
    public HeartRateMonitor()
    {
        _CBCentralManagerDelegate = new EmCBCentralManagerDelegate();
        _manager = new CBCentralManager(_CBCentralManagerDelegate, DispatchQueue.MainQueue);
    }
    public void Connect()
    {
        _manager.DiscoveredPeripheral += HandleDiscoveredPeripheral;
        _manager.ConnectedPeripheral += HandleConnectedPeripheral;
        _manager.DisconnectedPeripheral += HandleDisconnectedPeripheral;

        var guidsw = new CBUUID[] { CBUUID.FromString(BluetoothConstants.HEART_RATE_SERVICE) };
        _manager.ScanForPeripherals(guidsw, null);//Search for a peripheral that provides the HRM service
    }


    private void HandleDisconnectedPeripheral(object sender, CBPeripheralErrorEventArgs e)
    {
        Log("HandleDisconnectedPeripheral::" + e.Error);
    }

    public bool Logging { get; set; }
    private void Log(string message)
    {
        Debug.WriteLineIf(Logging, message);
    }

    /// <summary>
    /// Connected to the peripheral, set up event handlers and look for services
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HandleConnectedPeripheral(object sender, CBPeripheralEventArgs e)
    {
        _hrm.UpdatedCharacterteristicValue += Peripheral_UpdatedCharacterteristicValue;
        _hrm.DiscoverCharacteristic += Peripheral_DiscoverCharacteristic;
        _hrm.DiscoveredService += _hrm_DiscoveredService;
        _hrm.UpdatedNotificationState += _hrm_UpdatedNotificationState;
        _hrm.DiscoveredIncludedService += _hrm_DiscoveredIncludedService;

        var guidsw = new CBUUID[] { CBUUID.FromString(BluetoothConstants.HEART_RATE_SERVICE) };
        _hrm.DiscoverServices(guidsw);

    }

    private void _hrm_UpdatedNotificationState(object sender, CBCharacteristicEventArgs e)
    {
        CBCharacteristic characteristic = e.Characteristic;

        Log("_hrm_UpdatedNotificationState::" + characteristic.Description);
    }

    private void _hrm_DiscoveredIncludedService(object sender, CBServiceEventArgs e)
    {
        Log("_hrm_DiscoveredIncludedService::" + e.Service.Description);
    }
    /// <summary>
    /// Found the service, discover the service characteristics
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _hrm_DiscoveredService(object sender, NSErrorEventArgs e)
    {
        if (_hrm.Services.Length > 0)
        {
            CBService service = _hrm.Services[0];
            var guidsw = new CBUUID[] { };
            _hrm.DiscoverCharacteristics(guidsw, service);
            Log("_hrm_DiscoveredService::" + service.UUID);
        }
    }

    /// <summary>
    /// Discovered the Characteristics, tell the Peripheral what we're interested in
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Peripheral_DiscoverCharacteristic(object sender, CBServiceEventArgs e)
    {
        Log("Peripheral_DiscoverCharacteristic::Length:" + e.Service.Characteristics.Length);
        foreach (var characteristic in e.Service.Characteristics)
        {
            Log("Characteristic::" + characteristic.UUID);
            if (characteristic.UUID.ToString().ToUpper() == BluetoothConstants.HEART_RATE_MEASUREMENT)
            {
                _hrm.SetNotifyValue(true, characteristic);
                continue;
            }

            if (characteristic.UUID.ToString().ToUpper() == BluetoothConstants.BODY_SENSOR_LOCATION)
            {
                _hrm.ReadValue(characteristic);
                continue;
            }

            // Write heart rate control point
            if (characteristic.UUID.ToString().ToUpper() == BluetoothConstants.HEART_RATE_CONTROL_POINT)
            {
                var bytes = new byte[0];
                bytes[0] = 1;
                var data = NSData.FromArray(bytes);
                _hrm.WriteValue(data, characteristic, CBCharacteristicWriteType.WithResponse);
                continue;
            }
        }
    }


    /// <summary>
    /// When we get sent a value from the HRM
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Peripheral_UpdatedCharacterteristicValue(object sender, CBCharacteristicEventArgs e)
    {
        var characteristic = e.Characteristic;

        if (characteristic.UUID.ToString().ToUpper() == BluetoothConstants.HEART_RATE_MEASUREMENT && e.Characteristic.Value != null)
        {
            var dataBytes = e.Characteristic.Value.ToArray();
            int bpm = -1;
            if ((dataBytes[0] & 0x01) == 0)// the apple sample does some endian checks here...
            {
                bpm = dataBytes[1];
            }
            else
            {
                bpm = dataBytes[1];
            }
            OnHeartRateUpdated(new HeartRateEventArgs() { HeartRate = bpm });

        }
    }

    /// <summary>
    /// We've discovered a peripheral
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HandleDiscoveredPeripheral(object sender, CBDiscoveredPeripheralEventArgs e)
    {
        //this just assumes the peripheral is what we're after
        _hrm = e.Peripheral;
        var options = new NSDictionary();
        _manager.ConnectPeripheral(_hrm, options);
    }

    private class EmCBCentralManagerDelegate : CBCentralManagerDelegate
    {
        public override void UpdatedState(CBCentralManager central)
        {
            Log("State updated " + central.State);
        }
    }


}

