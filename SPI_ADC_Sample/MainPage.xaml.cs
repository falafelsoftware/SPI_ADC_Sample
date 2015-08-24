using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SPI_ADC_Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //using Windows.Devices.Spi;
        private SpiDevice _mcp3008;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //using SPI0 on the Pi
            var spiSettings = new SpiConnectionSettings(0);//for spi bus index 0
            spiSettings.ClockFrequency = 3600000; //3.6 MHz
            spiSettings.Mode = SpiMode.Mode0;

            string spiQuery = SpiDevice.GetDeviceSelector("SPI0");
            //using Windows.Devices.Enumeration;
            var deviceInfo = await DeviceInformation.FindAllAsync(spiQuery);
            if(deviceInfo!=null && deviceInfo.Count > 0)
            {
                _mcp3008 = await SpiDevice.FromIdAsync(deviceInfo[0].Id, spiSettings);
                btnConnect.IsEnabled = false;
            } else
            {
                txtHeader.Text = "SPI Device Not Found :-(";
            }
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            //From data sheet -- 1 byte selector for channel 0 on the ADC
            // First Byte sends the Start bit for SPI
            // Second Byte is the Configuration Byte
            //1 - single ended (this is where the 8 below is added)
            //0 - d2
            //0 - d1
            //0 - d0
            //             S321XXXX <-- single-ended channel selection configure bits
            // Channel 0 = 10000000 = 0x80 OR (8+channel) << 4
            // Third Byte is empty
            var transmitBuffer = new byte[3] { 1, 0x80, 0x00 };
            var receiveBuffer = new byte[3];

            _mcp3008.TransferFullDuplex(transmitBuffer, receiveBuffer);
            //first byte returned is 0 (00000000), 
            //second byte returned we are only interested in the last 2 bits 00000011 ( &3) 
            //shift 8 bits to make room for the data from the 3rd byte (makes 10 bits total)
            //third byte, need all bits, simply add it to the above result 
            var result = ((receiveBuffer[1] & 3) << 8) + receiveBuffer[2];
            //LM35 == 10mV/1degC ... 3.3V = 3300.0, 10 bit chip # steps is 2 exp 10 == 1024
            var mv = result * (3300.0 / 1024.0);
            var tempC = mv / 10.0;
            var tempF = (tempC * 9.0 / 5.0) + 32;

            var output = "The temperature is " + tempC + " Celsius\nand " + tempF + " Farenheit";
            txtReading.Text = output;
            
        }
    }
}
