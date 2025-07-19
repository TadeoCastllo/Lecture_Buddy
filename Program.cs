using System.Net.Http.Headers;
using System.Text;
using NAudio.Wave;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Interprete
{
    // Cargar claves y URLs desde variables de entorno
    private static readonly string apiKeyElevenLabs = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
    public static readonly string voiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID") ?? "1MxuWc12WPRxDkgfT3kj";
    private static readonly string apiKeyWatson = Environment.GetEnvironmentVariable("WATSON_API_KEY");
    private static readonly string urlWatson = Environment.GetEnvironmentVariable("WATSON_URL");
    private static readonly string voiceIdSpeechify = Environment.GetEnvironmentVariable("SPEECHIFY_VOICE_ID") ?? "charlotte";
    private static readonly string apiKeySpeechify = Environment.GetEnvironmentVariable("SPEECHIFY_API_KEY");

    // Método para convertir voz a texto usando FastAPI local
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
        return result;
    }

    // Método para convertir texto a voz usando FastAPI local
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
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        string url = "http://localhost:8000/text_to_speech";
        var response = await client.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error en el microservicio TTS local: {errorMessage}");
            return false;
        }
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

    // Watson y Speechify se mantienen igual, pero usando las variables de entorno
    public async Task AnalizarEmociones(string texto)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await ObtenerTokenIAM(apiKeyWatson));
            var response = await client.PostAsync(
                $"{urlWatson}/v1/analyze?version=2022-04-07",
                new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        text = texto,
                        features = new { emotion = new { document = true } },
                        language = "en"
                    }),
                    Encoding.UTF8,
                    "application/json"
                )
            );
            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                var emocionDominante = ((JObject)result.emotion.document.emotion)
                    .Properties()
                    .OrderByDescending(p => (double)p.Value)
                    .First();
                Console.WriteLine($"Emoción dominante: {emocionDominante.Name} ({emocionDominante.Value})");
            }
            else
            {
                Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    private async Task<string> ObtenerTokenIAM(string apiKey)
    {
        using (var httpClient = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://iam.cloud.ibm.com/identity/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "urn:ibm:params:oauth:grant-type:apikey" },
                { "apikey", apiKey }
            });
            var response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(json).access_token;
        }
    }

    public async Task<bool> ConvertirTextoAVozSpeechify(string texto, string salidaAudio)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKeySpeechify);
        var payload = new
        {
            input = texto,
            voice_id = voiceIdSpeechify,
            model = "simba-multilingual",
            audio_format = "mp3",
            language = "es-ES"
        };
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.sws.speechify.com/v1/audio/speech", content);
        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var audioResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            var audioBase64 = audioResponse.GetProperty("audio_data").GetString();
            byte[] audioBytes = Convert.FromBase64String(audioBase64);
            if (audioBytes.Length < 1024)
            {
                Console.WriteLine("El archivo de audio generado está vacío o corrupto.");
                return false;
            }
            await File.WriteAllBytesAsync(salidaAudio, audioBytes);
            Console.WriteLine($"Audio guardado en: {salidaAudio}");
            return true;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error en la API de Speechify: {response.StatusCode} - {error}");
            return false;
        }
    }
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
        using JsonDocument doc = JsonDocument.Parse(resultadoJson);
        string textoTranscrito = doc.RootElement.GetProperty("text").GetString();
        Console.WriteLine("📝 Texto transcrito: " + textoTranscrito);
        if (string.IsNullOrEmpty(textoTranscrito))
            return;
        await interprete.AnalizarEmociones(textoTranscrito);
        string salidaAudio = "salida.mp3";
        bool exito2 = await interprete.ConvertirTextoAVoz(textoTranscrito, salidaAudio);
        if (exito2)
            interprete.ReproducirAudio(salidaAudio);
    }
}