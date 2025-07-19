# tts_service.py
from elevenlabs import ElevenLabs
from dotenv import load_dotenv
import os

load_dotenv()

API_KEY = os.getenv("ELEVENLABS_API_KEY")
if not API_KEY:
    raise RuntimeError("No se encontró la variable ELEVENLABS_API_KEY")

client = ElevenLabs(api_key=API_KEY)

def texto_a_voz(
    texto: str,
    output_path: str,
    voice_id="WOY6pnQ1WCg0mrOZ54lM",
    model_id="eleven_monolingual_v1",
    stability=0.5,
    similarity_boost=0.75,
    style=0.0,
    speed=1.0
):
    response = client.text_to_speech.convert(
        voice_id=voice_id,
        output_format="mp3_44100_128",
        text=texto,
        model_id=model_id,
        voice_settings={
            "stability": stability,
            "similarity_boost": similarity_boost,
            "style": style,
            "speed": speed
        }
    )
    with open(output_path, "wb") as f:
        for chunk in response:
            f.write(chunk)

def listar_voces():
    voces = client.voices.search(include_total_count=True)
    return [{"voice_id": v.voice_id, "name": v.name, "labels": v.labels} for v in voces.voices]