using NoloClientCSharp;
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using VRE.Vridge.API.Client.Messages.BasicTypes;
using VRE.Vridge.API.Client.Messages.v3.Controller;
using VRE.Vridge.API.Client.Remotes;

namespace Vridge.NoloVR
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread m_Thread;
        private VridgeRemote m_VridgeRemote;
        private bool m_IsRunning;
        private bool m_RotationEnabled = false;
        private bool m_RudderEnabled = true;
        private float m_HeadHeight = 1.8f;

        public MainWindow()
        {
            m_VridgeRemote = new VridgeRemote("localhost", "Vridge.NoloVR", Capabilities.HeadTracking | Capabilities.Controllers);

            InitializeComponent();

            SetTextValue(HeadHeight, m_HeadHeight);
            SetToggleValue(HeadRotationEnabled, m_RotationEnabled);
            SetToggleValue(NoloVREnabled, m_RudderEnabled);

            Closing += Window_Closed;
        }

        private void SetTextValue(TextBox textBox, object value) => textBox.Text = value.ToString();
        private void SetToggleValue(CheckBox box, bool value) => box.IsChecked = value;

        public void Start()
        {
            Stop();

            var path = @"NoloServer\NoloServer.exe";
            //if (File.Exists(path))
                //NoloClientLib.StartNoloServer(@"NoloServer\NoloServer.exe");

            m_IsRunning = NoloClientLib.OpenNoloZeroMQ();
    
            if (m_IsRunning)
            {
                m_Thread = new Thread(new ThreadStart(ThreadLoop));
                m_Thread.Start();

                StatusLabel.Text = "Started";
            }
            else
                StatusLabel.Text = "Not Started";
        }

        public void Stop()
        {
            m_IsRunning = false;

            if (m_Thread != null)
            {
                if (m_Thread.IsAlive)
                    m_Thread.Abort();

                m_Thread = null;
            }

            StatusLabel.Text = "Stopped";
        }

        private void Recenter()
        {
            var pos = new NVector3()
            {
                x = 0,
                y = m_HeadHeight,
                z = 0
            };

            NoloClientLib.SetHmdCenter(ref pos);
        }

        private void ThreadLoop()
        {
            while (m_IsRunning)
            {
                if (m_RudderEnabled)
                {
                    if (m_VridgeRemote.Head != null)
                    {
                        var hmd = NoloClientLib.GetHMDData();
                        var pos = hmd.HMDPosition;

                        if (m_RotationEnabled)
                        {
                            var rot = hmd.HMDRotation;
                            m_VridgeRemote.Head.SetRotationAndPosition(0, 0, 0, pos.x, pos.y, pos.z);
                        }
                        else
                            m_VridgeRemote.Head.SetPosition(pos.x, pos.y, pos.z);
                    }

                    if (m_VridgeRemote.Controller != null)
                    {
                        UpdateController(true);
                        UpdateController(false);
                    }
                }

                Thread.Sleep(10);
            }
        }

        private void UpdateController(bool left)
        {
            var controller = left ? NoloClientLib.GetLeftControllerData() : NoloClientLib.GetRightControllerData();
            var position = controller.Position;
            var rotation = controller.Rotation;
           
            m_VridgeRemote.Controller.SetControllerState(
                left ? 0 : 1,
                HeadRelation.IsInHeadSpace,
                left ? HandType.Left : HandType.Right,
                new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w),
                new Vector3(position.x, position.y, position.z),
                controller.TouchAxis.x,
                controller.TouchAxis.y,
                (controller.Buttons & (uint)EControlerButtonType.eTriggerBtn) > 0 ? 1 : 0,
                (controller.Buttons & (uint)EControlerButtonType.eMenuBtn) > 0,
                (controller.Buttons & (uint)EControlerButtonType.eSystemBtn) > 0,
                (controller.Buttons & (uint)EControlerButtonType.eTriggerBtn) > 0,
                (controller.Buttons & (uint)EControlerButtonType.eGripBtn) > 0,
                (controller.Buttons & (uint)EControlerButtonType.ePadTouch) > 0,
                controller.Touched > 0);
        }

        private void OnConnectClicked(object sender, RoutedEventArgs e) => Start();
        private void OnDisconnectClicked(object sender, RoutedEventArgs e) => Stop();
        private void OnRecenterClicked(object sender, RoutedEventArgs e) => Recenter();

        private void Window_Closed(object sender, EventArgs e)
        {
            Stop();
            Application.Current.Shutdown();
        }

        private void HandleCheckbox(object sender, ref bool target)
        {
            var checkbox = (CheckBox)sender;

            if (checkbox.IsChecked.HasValue)
                target = checkbox.IsChecked.Value;
            else
                target = false;
        }

        private void HandleTextBoxFloat(object sender, ref float target)
        {
            var text = (TextBox)sender;

            if (float.TryParse(text.Text, out float result))
                target = result;
        }

        private void OnNoloVREnabledChanged(object sender, RoutedEventArgs e)
        {
            HandleCheckbox(sender, ref m_RudderEnabled);
        }

        private void OnHeadRotationChanged(object sender, RoutedEventArgs e)
        {
            HandleCheckbox(sender, ref m_RotationEnabled);
        }

        private void HeadHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            HandleTextBoxFloat(sender, ref m_HeadHeight);
        }
    }
}
