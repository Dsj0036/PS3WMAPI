using System;
using System.Windows.Forms;

namespace PS3WMAPI_Example
{
    public partial class Form1 : Form
    {
        PS3WMAPI.PS3.Client _client;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _client = new PS3WMAPI.PS3.Client(textBox1.Text, "PS3(TEST)", 5000, true);
            _client.OnUpdateReceived += _client_OnUpdateReceived;
            _client.OnProgressReport += _client_OnProgressReport;
            _client.OnClientHeartbeat += _client_OnClientHeartbeat;
            ButtonClient.Enabled = false;
            ButtonConnect.Enabled = true;
        }
        void invokea(Action e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                e();
            });
        }
        private void _client_OnProgressReport(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            invokea(() =>
            {
                this.ProgressBar1.Value = e.ProgressPercentage;
            });
        }

        private void _client_OnClientHeartbeat(object sender, int e)
        {
            invokea(() =>
            {

                label();
            });
        }

        private void _client_OnUpdateReceived(object sender, PS3WMM e)
        {
            invokea(() =>
            {

                label();
            });
        }
        void label() => LabelStatus.Text = string.Format("Initialized: {0}\nConnected: {1}\nAttached: {2}\nTicks: {3}", _client.Running, _client.IsConnected, _client.Ready ? _client.MapiServer.IsAttached : false, _client.RunTime);

        private void button3_Click(object sender, EventArgs e)
        {
            _client.Initialize();
            ButtonConnect.Enabled = false;
            ButtonDisconnect.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _client.Dispose();
            ButtonDisconnect.Enabled = false;
            ButtonClient.Enabled = true;
            ButtonConnect.Enabled = false;
        }
    }
}
