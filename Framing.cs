using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialToHttpPoC
{
    public sealed class Framing
    {
        private enum Estado
        {
            ESPERANDO_ENCABEZADO,
            LEYENDO_ENCABEZADO,
            ESPERANDO_TERMINADOR,
            LEYENDO_TERMINADOR,
            MENSAJE_LEIDO
        }

        private const int DEFAULT_TRIES = 3;

        private const int FRACTIONS_PER_TRY = 10;

        private static int intentos = DEFAULT_TRIES;

        private Framing()
        {
        }

		static int Tries
        {
			get { return intentos; }
			set { intentos = value;  }
        }

		public static String Read(Stream input, String header, String end, int timeout)
		{
			if (intentos == 0) 
			{
				intentos = DEFAULT_TRIES;
			}

			StringBuilder buffer = new StringBuilder();
			int largoEncabezado = header.Length;
			int largoTerminador = end.Length;
			if (largoEncabezado == 0 || largoTerminador == 0)
			{
				throw new ArgumentException("El header y el end del mensaje no pueden ser vacíos.");
			}

			// ya no salimos antes porque en el primer intento no llegue el header
			// pero puedo tener llamadores que me pasan el timeout en negativo así que
			// aquí lo dejo positivo. enrico. 21/11/17.
			if (timeout < 0)
			{
				timeout = -timeout;
			}

			// como vamos a intentar 10 veces en completar el mensaje con lo que venga desde
			// la corriente de datos entonces vamos a ir esperando timeout / 10 cada vez.
			int miniTimeout = timeout / FRACTIONS_PER_TRY;
			if (miniTimeout < 10)
			{
				miniTimeout = 0;
			}

			Estado estado = Estado.ESPERANDO_ENCABEZADO;
			int indice;
			char letra;
			int dato;
			bool inicioEncabezado;
			for (int i = 0; i < FRACTIONS_PER_TRY * intentos && estado != Estado.MENSAJE_LEIDO; i++)
			{
				while (estado != Estado.MENSAJE_LEIDO && (dato = input.ReadByte()) > -1)
				{
					letra = (char)dato;

					inicioEncabezado = letra == header[0];
					if (inicioEncabezado && estado != Estado.ESPERANDO_ENCABEZADO)
					{
						buffer = new StringBuilder();
						estado = Estado.ESPERANDO_ENCABEZADO;
					}

					switch (estado)
					{
						case Estado.ESPERANDO_ENCABEZADO:
							// simplemente cuando encontramos el primer caracter del header
							// nos pasaremos al estado leyendo header. 
							if (inicioEncabezado)
							{
								if (largoEncabezado > 1)
								{
									estado = Estado.LEYENDO_ENCABEZADO;
								}
								else
								{
									estado = Estado.ESPERANDO_TERMINADOR;
								}
								buffer.Append(letra);
							}
							break;

						case Estado.LEYENDO_ENCABEZADO:
							// salimos de este estado solo cuando encontremos en la corriente de datos 
							// el header completo. cuando pasamos al estado esperando end 
							// quitamos la parte inicial del buffer que puede estar sobrando.
							buffer.Append(letra);
							indice = buffer.ToString().IndexOf(header);
							if (indice >= 0)
							{
								estado = Estado.ESPERANDO_TERMINADOR;
								if (indice > 0)
								{
									buffer.Remove(0, indice);
								}
							}
							break;

						case Estado.ESPERANDO_TERMINADOR:
							// en este momento buscamos la primer letra del end.
							buffer.Append(letra);
							if (letra == end[0])
							{
								if (largoTerminador > 1)
								{
									estado = Estado.LEYENDO_TERMINADOR;
								}
								else
								{
									estado = Estado.MENSAJE_LEIDO;
								}
							}
							break;

						case Estado.LEYENDO_TERMINADOR:
							// salimos de este estado solo cuando encontremos en la corriente de datos
							// el end completo. notar que estoy buscando el end a partir
							// del caracter que viene a continuación del header.
							buffer.Append(letra);
							indice = buffer.ToString().LastIndexOf(end);
							if (indice >= largoEncabezado)
							{
								estado = Estado.MENSAJE_LEIDO;
							}
							break;

						default:
							break;
					}
				}
				if (miniTimeout > 0 && estado != Estado.MENSAJE_LEIDO)
				{
					try
					{
						Thread.Sleep(miniTimeout);
					}
					catch (ThreadInterruptedException)
					{
						Thread.CurrentThread.Interrupt();
						break;
					}
				}
			}

			return (estado == Estado.MENSAJE_LEIDO) ? buffer.ToString() : null; 
		}
    }
}
