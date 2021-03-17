using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToHttp
{
    public sealed class Framing
    {
        private enum State
        {
            WAITING_HEADER,
            READING_HEADER,
            WAITING_TERMINATOR,
            READING_TERMINATOR,
            MESSAGE_READ
        }

        private const int TIMEOUT_FRACTIONS = 10;

        private const int MIN_TIMEOUT = 50;

        private SerialPort port;

        private String header;

        private String terminator;

        private int headerLen;

        private int terminatorLen;

        private int timeout;

        private bool console;

        public Framing(SerialPort port, String header, String terminator, int timeout)
        {
            headerLen = header.Length;
            terminatorLen = terminator.Length;
            if (headerLen == 0 || terminatorLen == 0)
            {
                throw new ArgumentException("Neither Header nor terminator cannot be empty!");
            }

            if (timeout < 0)
            {
                timeout = 0;
            }
            timeout /= TIMEOUT_FRACTIONS;
            if (timeout < MIN_TIMEOUT)
            {
                timeout = MIN_TIMEOUT;
            }

            this.port = port;
            this.header = header;
            this.terminator = terminator;
            this.timeout = timeout;
            this.console = Environment.UserInteractive;
        }

		public String Read()
		{
            port.ReadTimeout = timeout;

            StringBuilder buffer = new StringBuilder();
			State state = State.WAITING_HEADER;
			int index;
			int byteRead;
            char charRead;
            bool headerStart;
			for (int i = 0; i < TIMEOUT_FRACTIONS && state != State.MESSAGE_READ; i++)
			{
                try
                {
                    while (state != State.MESSAGE_READ && (byteRead = port.ReadByte()) > -1)
                    {
                        charRead = (char) byteRead;

                        if (console)
                        {
                            System.Console.WriteLine("New char received: " + charRead + "\tAscii value = " + (int)charRead);
                        }

                        headerStart = charRead == header[0];
                        if (headerStart && state != State.WAITING_HEADER)
                        {
                            buffer = new StringBuilder();
                            state = State.WAITING_HEADER;
                        }

                        switch (state)
                        {
                            case State.WAITING_HEADER:
                                // simplemente cuando encontramos el primer caracter del header
                                // nos pasaremos al estado leyendo header. 
                                if (headerStart)
                                {
                                    if (headerLen > 1)
                                    {
                                        state = State.READING_HEADER;
                                    }
                                    else
                                    {
                                        state = State.WAITING_TERMINATOR;
                                    }
                                    buffer.Append(charRead);
                                }
                                break;

                            case State.READING_HEADER:
                                // salimos de este estado solo cuando encontremos en la corriente de datos 
                                // el header completo. cuando pasamos al estado esperando end 
                                // quitamos la parte inicial del buffer que puede estar sobrando.
                                buffer.Append(charRead);
                                index = buffer.ToString().IndexOf(header);
                                if (index >= 0)
                                {
                                    state = State.WAITING_TERMINATOR;
                                    if (index > 0)
                                    {
                                        buffer.Remove(0, index);
                                    }
                                }
                                break;

                            case State.WAITING_TERMINATOR:
                                // en este momento buscamos la primer letra del end.
                                buffer.Append(charRead);
                                if (charRead == terminator[0])
                                {
                                    if (terminatorLen > 1)
                                    {
                                        state = State.READING_TERMINATOR;
                                    }
                                    else
                                    {
                                        state = State.MESSAGE_READ;
                                    }
                                }
                                break;

                            case State.READING_TERMINATOR:
                                // salimos de este estado solo cuando encontremos en la corriente de datos
                                // el end completo. notar que estoy buscando el end a partir
                                // del caracter que viene a continuación del header.
                                buffer.Append(charRead);
                                index = buffer.ToString().LastIndexOf(terminator);
                                if (index >= headerLen)
                                {
                                    state = State.MESSAGE_READ;
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // next try
                }
				catch (ThreadInterruptedException)
				{
					Thread.CurrentThread.Interrupt();
					break;
				}
			}

            return (state == State.MESSAGE_READ) ? buffer.ToString() : null; 
		}
    }
}
