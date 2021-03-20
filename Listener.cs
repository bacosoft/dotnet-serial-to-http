using System;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace SerialToHttp
{
	public class Listener
	{
        private const string CONTENT_TYPE = "text/plain; charset=utf-8";

        private const string URL_PREFIX_KEY = "urlPrefix";
		private const string DEFAULT_URL_PREFIX = "http://localhost:8080/serialToHttp/";

        private const string TEST_PORT_NAME = "test";
        private const string TEST_RESPONSE_KEY = "testResponse";
        private const string DEFAULT_TEST_RESPONSE = "test";

        private const string PORT_NAME_KEY = "portName";
		private const string DEFAULT_PORT_NAME = TEST_PORT_NAME;
		private const string BAUD_RATE_KEY = "baudRate";
		private const string PARITY_KEY = "parity";
		private const string DATA_BITS_KEY = "dataBits";
		private const string STOP_BITS_KEY = "stopBits";
		private const string HAND_SHAKE_KEY = "handshake";

		private const string QUERY_KEY = "query";
		private const string DEFAULT_QUERY = "query";

        private const string PATTERN_KEY = "pattern";
        private const string SUBSTITUTION_KEY = "substitution";

        private const string HEADER_KEY = "header";
		private const string DEFAULT_HEADER = "[";
		private const string TERMINATOR_KEY = "terminator";
		private const string DEFAULT_TERMINATOR = "]";

        private const string HEX_ENCODED = "hex:";

        private const int DEFAULT_TIMEOUT = 2000;

		private HttpListener listener;

        private bool connected = false;

		private SerialPort port;

        private Framing framing;

		private CancellationTokenSource cancellationTokenSource;

        private string query;

        private string testResponse;

        private Regex regex;

        private string substitution;

        public Listener()
		{
		}

		public void Start()
		{
			string prefix = GetProperty(URL_PREFIX_KEY, DEFAULT_URL_PREFIX);
			listener = new HttpListener();
			listener.Prefixes.Add(prefix);
			listener.Start();

			cancellationTokenSource = new CancellationTokenSource();
			ThreadPool.QueueUserWorkItem(new WaitCallback(Listen), cancellationTokenSource.Token);
		}

		private void Listen(object obj)
		{
			CancellationToken token = (CancellationToken) obj;
			while (!token.IsCancellationRequested)
			{
                ProcessRequest(listener.GetContext());
			}
		}

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpStatusCode code;
            string message;
            try
            {
                message = Query();
                if (message != null)
                {
                    code = HttpStatusCode.OK;
                }
                else
                {
                    // nothing read after a while, returning 404 instead of 500
                    message = "";
                    code = HttpStatusCode.NotFound;
                }
            }
            catch (Exception e)
            {
                message = e.Message + "\n\n" + e.StackTrace;
                code = HttpStatusCode.InternalServerError;
            }
            WriteResponse(context.Response, code, message);
        }

        private void WriteResponse(HttpListenerResponse response, HttpStatusCode code, string message)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            response.StatusCode = (int) code;
            response.ContentLength64 = buffer.Length;
            response.ContentType = CONTENT_TYPE;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        private void Connect()
        {
            string header = GetProperty(HEADER_KEY, DEFAULT_HEADER);
            string terminator = GetProperty(TERMINATOR_KEY, DEFAULT_TERMINATOR);
            query = GetProperty(QUERY_KEY, DEFAULT_QUERY);

            String pattern = GetProperty(PATTERN_KEY);
            if (pattern != null)
            {
                regex = new Regex(pattern, RegexOptions.Compiled);
                substitution = GetProperty(SUBSTITUTION_KEY);
            }

            port = CreatePort();
            if (port != null)
            {
                // real serial port
                port.Open();

                // framing handler
                framing = new Framing(port, header, terminator, DEFAULT_TIMEOUT);
            }
            else
            {
                // emulation mode
                testResponse = GetProperty(TEST_RESPONSE_KEY, DEFAULT_TEST_RESPONSE);
            }
            connected = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private string Query()
		{
            if (!connected)
            {
                // deferred connection to serial port
                Connect();
            }
            string res;
            if (port != null)
            {
                // real serial port
                port.Write(query);
                res = framing.Read();
            }
            else
            {
                // emulation mode
                res = testResponse;
            }
            if (regex != null)
            {
                if (!regex.IsMatch(res))
                {
                    throw new ArgumentException("Invalid value: " + res);
                }
                else
                {
                    res = regex.Replace(res, substitution);
                }
            }
            return res;
		}

		private SerialPort CreatePort()
		{
            SerialPort port = null;

            string portName = GetProperty(PORT_NAME_KEY, DEFAULT_PORT_NAME);
            if (!portName.Equals(TEST_PORT_NAME))
            {
                port = new SerialPort(portName);

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
            }
			return port;
		}

		public void Stop()
		{
			if (listener != null)
			{
				cancellationTokenSource.Cancel();
				listener.Stop();
				listener = null;
                if (port != null)
                {
                    port.Close();
                    port = null;
                }
			}
		}

		private static string GetProperty(string key, string defaultValue)
		{
			string value = ConfigurationManager.AppSettings[key];
			if (value == null)
			{
				value = defaultValue;
			}
            if (value != null && value.StartsWith(HEX_ENCODED))
            {
                value = StringUtils.DecodeHex(value.Substring(HEX_ENCODED.Length));
            }
			return value;
		}

        private static string GetProperty(string key)
		{
			return GetProperty(key, null);
		}
    }
}