using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Win32;

namespace LokiAudio
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("partialsourcename partialtargetname");
                return;
            }

            var sourceName = args[0];
            var targetName = args[1];
            var captureMode = CaptureMode.LoopbackCapture;
            MMDevice sourceDevice = null;
            MMDevice targetDevice = null;
            using (var deviceEnumerator = new MMDeviceEnumerator())
            using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(
                captureMode == CaptureMode.Capture ? DataFlow.Capture : DataFlow.Render, DeviceState.Active))
            {
                foreach (var device in deviceCollection)
                {
                    if (sourceDevice == null && DeviceMatches(device, sourceName))
                    {
                        Console.WriteLine("Dispositivo principal: " + device.FriendlyName);
                        sourceDevice = device;
                    }
                    if (targetDevice == null && DeviceMatches(device, targetName))
                    {
                        targetDevice = device;
                        Console.WriteLine("Dispositivo secundário: " + device.FriendlyName);
                    }
                }
            }

            if (sourceDevice == targetDevice)
            {
                Console.WriteLine("Dispositivo principal e secundário são os mesmos. Encerrando aplicação.");
            }

            Console.WriteLine("Iniciando captura");

            StartCapture(sourceDevice, targetDevice);
        }

        private static void StartCapture(MMDevice sourceDevice, MMDevice targetDevice)
        {
            // Inicializa a captura de áudio no dispositivo de origem
            var soundIn = new WasapiLoopbackCapture { Device = sourceDevice };
            soundIn.Initialize();

            // Inicializa a saída de áudio no dispositivo de destino
            var soundOut = new WasapiOut() { Latency = 200, Device = targetDevice };

            try
            {
                // Força a inicialização e cria o fluxo de áudio para o WasapiOut
                soundOut.Initialize(new SoundInSource(soundIn));
                Console.WriteLine("WasapiOut inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao inicializar a saída de áudio: " + ex.Message);
                return;
            }

            // Inicia a captura e a reprodução do áudio
            soundIn.Start();
            soundOut.Play();

            // Exibe uma mensagem de confirmação de que o áudio está sendo transmitido
            Console.WriteLine("Captura e reprodução de áudio em andamento...");

            // Variáveis de controle de tempo e execução
            DateTime startTime = DateTime.Now;
            TimeSpan timeoutDuration = TimeSpan.FromMinutes(1440);  // Tempo fixo de 1440 minutos (24 horas)

            // Variável de contador para exibir quantas vezes o áudio foi "Playing"
            int playingCount = 0;

            // Variável para controlar a execução do loop
            bool audioPlaying = true;
            bool wasPaused = false;

            // Monitorar a captura e reprodução de áudio
            while (audioPlaying)
            {
                // Verifica se o tempo de execução atingiu o limite
                if (DateTime.Now - startTime > timeoutDuration)
                {
                    Console.WriteLine("Limite de tempo atingido, encerrando a execução...");
                    break;
                }

                // Verifica o estado da reprodução
                if (soundOut.PlaybackState == PlaybackState.Playing)
                {
                    playingCount++;
                    Console.Write($"\rCurrent playback state: Playing [{playingCount}]");

                    // Se o áudio estava pausado e agora está tocando, sinaliza que o áudio voltou
                    if (wasPaused)
                    {
                        Console.WriteLine("\nÁudio voltou a tocar!");
                        wasPaused = false;  // Resetamos o estado de "pausado"
                    }
                }
                else
                {
                    // Se o áudio estiver pausado ou parado, registramos o evento
                    if (!wasPaused)
                    {
                        Console.Write("\rCurrent playback state: Paused");
                        wasPaused = true;  // Marca que o áudio entrou em pausa
                    }

                    // Se o áudio foi pausado ou parado, tentamos retomar a reprodução
                    if (soundOut.PlaybackState == PlaybackState.Paused || soundOut.PlaybackState == PlaybackState.Stopped)
                    {
                        soundOut.Play();  // Força a retomada da reprodução
                        Console.Write("\rTentando retomar a reprodução...");
                    }
                }

                // Aguardar para evitar uso excessivo de CPU
                Thread.Sleep(100);

                // Verificar se o usuário deseja parar a execução
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("\nDesligando o áudio e encerrando a aplicação...");
                    break;
                }
            }

            // Finaliza a captura e reprodução quando o loop é interrompido
            soundIn.Stop();
            soundOut.Stop();
            Console.WriteLine("\nCaptura e reprodução de áudio finalizada.");
        }


        private static bool DeviceMatches(MMDevice device, string name)
        {
            return device.FriendlyName.ToLower().Contains(name.ToLower());
        }

        public enum CaptureMode
        {
            Capture,
            LoopbackCapture
        }
    }
}
