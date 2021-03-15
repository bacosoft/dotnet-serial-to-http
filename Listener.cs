using System;
using System.Configuration;
using System.IO.Ports;
using System.Net;
using System.Threading;

public class Listener
{
	private const string URL_PREFIX_KEY = "urlPrefix";
	private const string DEFAULT_URL_PREFIX = "http://localhost:8080/serialToHttp/";

	private const string PORT_NAME_KEY = "portName";
	private const string DEFAULT_PORT_NAME = "COM4";
	private const string BAUD_RATE_KEY = "baudRate";
	private const string PARITY_KEY = "parity";
	private const string DATA_BITS_KEY = "dataBits";
	private const string STOP_BITS_KEY = "stopBits";
	private const string HAND_SHAKE_KEY = "handshake";

	private HttpListener listener;

	private SerialPort port;

	private CancellationTokenSource cancellationTokenSource;

	private string lastValue;

	public Listener()
	{
	}

	public void Start()
	{
		string prefix = GetProperty(URL_PREFIX_KEY, DEFAULT_URL_PREFIX);
		listener = new HttpListener();
		listener.Prefixes.Add(prefix);
		listener.Start();

		port = CreatePort();
		port.Open();

		cancellationTokenSource = new CancellationTokenSource();
		ThreadPool.QueueUserWorkItem(new WaitCallback(listen), cancellationTokenSource.Token);
	}

	private void listen(object obj)
	{
		CancellationToken token = (CancellationToken)obj;

		while (!token.IsCancellationRequested)
		{
			HttpListenerContext context = listener.GetContext();
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			string responseString = lastValue ?? "";
			if (string.IsNullOrEmpty(responseString))
            {
				responseString = "No hay datos";
            }
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer, 0, buffer.Length);
			output.Close();
		}
	}

	private SerialPort CreatePort()
	{
		SerialPort port = new SerialPort(GetProperty(PORT_NAME_KEY, DEFAULT_PORT_NAME));

		string property = GetProperty(BAUD_RATE_KEY);
		if (!string.IsNullOrEmpty(property))
		{
			port.BaudRate = int.Parse(property);
		}

		property = GetProperty(PARITY_KEY);
		if (!string.IsNullOrEmpty(property))
		{

			port.Parity = (Parity)Enum.Parse(typeof(Parity), property);
		}

		property = GetProperty(DATA_BITS_KEY);
		if (!string.IsNullOrEmpty(property))
		{
			port.DataBits = int.Parse(property);
		}

		property = GetProperty(STOP_BITS_KEY);
		if (!string.IsNullOrEmpty(property))
		{
			port.StopBits = (StopBits)Enum.Parse(typeof(StopBits), property);
		}

		property = GetProperty(HAND_SHAKE_KEY);
		if (!string.IsNullOrEmpty(property))
		{
			port.Handshake = (Handshake)Enum.Parse(typeof(Handshake), property);
		}

		port.DataReceived += new SerialDataReceivedEventHandler(dataReceived);
		return port;
	}

	private void dataReceived(object sender, SerialDataReceivedEventArgs e)
    {
		lastValue = port.ReadExisting();
    }

	public void Stop()
	{
		if (listener != null)
		{
			cancellationTokenSource.Cancel();
			listener.Stop();
			listener = null;
			port.Close();
			port = null;
		}
	}

	private static string GetProperty(string key, string defaultValue)
	{
		string value = ConfigurationManager.AppSettings[key];
		if (value == null)
		{
			value = defaultValue;
		}
		return value;
	}

	private static string GetProperty(string key)
	{
		return GetProperty(key, null);
	}
}
