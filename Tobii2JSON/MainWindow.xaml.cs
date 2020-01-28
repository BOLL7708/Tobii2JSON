using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using Tobii.Interaction;

namespace Tobii2JSON
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Host host;
        private Thread t;
        private double x = 0, y = 0, ts = 0;
        private int port = 7081;

        public MainWindow()
        {
            InitializeComponent();

            // Init tobii host and fetch stream, requires runtime to be active
            host = new Host();
            var gazePointDataStream = host.Streams.CreateGazePointDataStream();

            // This will continiously update the gaze data
            gazePointDataStream.GazePoint((x, y, ts) =>
            {
                this.x = x;
                this.y = y;
                this.ts = ts;
                // Debug.WriteLine("X: " + x + ", Y: " + y + ", TS: " + ts);
            });

            if (t == null) t = new Thread(WorkerThread);
            t.Start();
        }

        private void WorkerThread()
        {
            // Init listeners on the specified hosts and port
            Thread.CurrentThread.IsBackground = true;
            var prefixes = new String[] { "http://localhost:" + port + "/", "http://127.0.0.1:" + port + "/" };
            HttpListener listener = new HttpListener();
            foreach (String s in prefixes)
            {
                try
                {
                    listener.Prefixes.Add(s);
                    Debug.WriteLine("Added HTTP listener (" + port + ") to " + s + " successfully.");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed adding HTTP listener (" + port + "): " + e.Message);
                }
            }
            listener.Start();

            // Listener loop
            while (true)
            {
                try
                {
                    var context = listener.GetContext(); // Locks thread until request

                    // Generate JSON
                    var responseString = "{\"ts\":\"" + ts + "\", \"x\":\"" + x + "\", \"y\":\"" + y + "\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                    // Create response
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;

                    // Output response
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
                catch (HttpListenerException e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        private void OnApplicationExit()
        {
            // we will close the coonection to the Tobii Engine before exit. 
            t.Abort();
            host.DisableConnection();
        }
    }
}
