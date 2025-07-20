using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Interprete
{
    public static readonly string voiceId = "1MxuWc12WPRxDkgfT3kj"; // ID de la voz a utilizar en Eleven Labs
    private static readonly string voiceIdSpeechify = "charlotte"; // ID de la voz a utilizar en Speechify (puedes cambiarlo según tus preferencias)

    // Método para convertir voz a texto usando OpenAI Whisper
    public async Task<string> ConvertirVozATexto(string rutaArchivo)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        var form = new MultipartFormDataContent
    {
        { new StreamContent(File.OpenRead(rutaArchivo)), "file", Path.GetFileName(rutaArchivo) }
    };

        var response = await client.PostAsync("http://localhost:8000/transcribe_audio", form);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error al llamar a la API local: {result}");
            return $"Error: {result}";
        }

        return result; // Devuelve el JSON crudo, sin deserializar
    }

    // Método para convertir texto a voz usando Eleven Labs
    public async Task<bool> ConvertirTextoAVoz(string texto, string salidaAudio)
    {
        using var client = new HttpClient();
        var payload = new
        {
            text = texto,
            voice_id = voiceId,
            model_id = "eleven_monolingual_v1",
            stability = 0.5,
            similarity_boost = 0.75,
            style = 0.0,
            speed = 1.0
        };
        string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://localhost:8000/text_to_speech", content);
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error en el endpoint local de ElevenLabs: {errorMessage}");
            return false;
        }
        // Guardar el archivo MP3 recibido
        using (var fs = new FileStream(salidaAudio, FileMode.Create, FileAccess.Write))
        {
            await response.Content.CopyToAsync(fs);
        }
        Console.WriteLine($"Audio guardado en: {salidaAudio}");
        return true;
    }

    // Método para reproducir el archivo de audio en MP3
    public void ReproducirAudio(string rutaAudio)
    {
        if (!File.Exists(rutaAudio))
        {
            Console.WriteLine("Error: El archivo de audio no existe.");
            return;
        }

        using (var reader = new Mp3FileReader(rutaAudio))
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(reader);
            outputDevice.Play();

            Console.WriteLine("Reproduciendo audio... (Presiona Enter para detener)");
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    outputDevice.Stop();
                    break;
                }
                System.Threading.Thread.Sleep(100);
            }
        }
    }
    public async Task AnalizarEmociones(string texto)
    {
        using var client = new HttpClient();
        var payload = new { text = texto };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://localhost:8000/analizar_emocion_hf", content);
        var result = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            // El endpoint devuelve un JSON con la emoción principal y score
            Console.WriteLine($"Resultado de emociones HuggingFace: {result}");
        }
        else
        {
            Console.WriteLine($"Error en el endpoint de emociones HF: {result}");
        }
    }

    // 🔊 Text-to-Speech con Speechify

    public async Task<bool> ConvertirTextoAVozSpeechify(string texto, string salidaAudio)
    {
        using var client = new HttpClient();
        var payload = new
        {
            text = texto,
            voice_id = voiceIdSpeechify,
            audio_format = "mp3",
            language = "es-ES",
            model = "simba-multilingual",
            pitch = (string)null,
            rate = (string)null,
            emotion = (string)null
        };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://localhost:8000/text_to_speech_speechify", content);
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error en el endpoint local de Speechify: {errorMessage}");
            return false;
        }
        // Guardar el archivo MP3 recibido
        using (var fs = new FileStream(salidaAudio, FileMode.Create, FileAccess.Write))
        {
            await response.Content.CopyToAsync(fs);
        }
        Console.WriteLine($"Audio guardado en: {salidaAudio}");
        return true;
    }



    class Program
    {
        static async Task Main(string[] args)
        {
            Interprete interprete = new Interprete();

            string rutaAudio = "Guardia.wav";
            string resultadoJson = await interprete.ConvertirVozATexto(rutaAudio);
            Console.WriteLine("🔍 Resultado recibido:");
            Console.WriteLine(resultadoJson);

            if (resultadoJson.StartsWith("Error:"))
            {
                Console.WriteLine(resultadoJson);
                return;
            }

            // Ahora sí, parsea el JSON
            using JsonDocument doc = JsonDocument.Parse(resultadoJson);
            string textoTranscrito = doc.RootElement.GetProperty("text").GetString();
            Console.WriteLine("📝 Texto transcrito: " + textoTranscrito);

            if (string.IsNullOrEmpty(textoTranscrito))
                return;

            await interprete.AnalizarEmociones(textoTranscrito);

            //            string salidaAudio = "salida.mp3";

            // 🎙 OPCIÓN 1: Usar Speechify como motor TTS
            //            bool exito = await interprete.ConvertirTextoAVozSpeechify(textoTranscrito, salidaAudio);

            // 🎙 OPCIÓN 2: Usar ElevenLabs como motor TTS
            //bool exito2 = await interprete.ConvertirTextoAVoz(textoTranscrito, salidaAudio);

            //            if (exito)
            //              interprete.ReproducirAudio(salidaAudio);
        }
    }
}