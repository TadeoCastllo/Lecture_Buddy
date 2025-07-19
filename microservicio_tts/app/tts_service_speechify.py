import os
import base64
from dotenv import load_dotenv
from speechify.client import Speechify
from typing import Optional
load_dotenv()

SPEECHIFY_TOKEN = os.getenv("SPEECHIFY_API_KEY")
if not SPEECHIFY_TOKEN:
    raise RuntimeError("No se encontró la variable SPEECHIFY_API_KEY en el entorno")

client = Speechify(token=SPEECHIFY_TOKEN)

# Diccionario de estilos por ID de voz personalizado
VOICE_STYLES = {
    "alejandro": {"emotion": "direct", "pitch": "+20%"},
    "valeria": {"emotion": "energetic", "pitch": "+25%"},
    "alexa": {"emotion": "relaxed", "pitch": "+10%"},
    "carmen": {"emotion": "energetic", "pitch": "+15%"},
    "alondra": {"emotion": "energetic", "pitch": "+25%", "rate": "+2%"},
    "daniela": {"emotion": "energetic", "pitch": "+5%", "rate": "-4%"},
    "maximiliano": {"emotion": "energetic", "pitch": "-3%", "rate": "+2%"},
}

def listar_voces_speechify():
    voces = client.tts.voices.list()
    return [{"voice_id": v.id, "name": v.display_name, "gender": v.gender} for v in voces]

def construir_ssml(texto: str, voice_id: str, pitch: Optional[str], rate: Optional[str], emotion: Optional[str]) -> str:
    estilo = VOICE_STYLES.get(voice_id, {})

    # Si se pasa "string", None, o "", se ignora
    pitch = pitch if pitch not in [None, "", "string"] else estilo.get("pitch", "medium")
    rate = rate if rate not in [None, "", "string"] else estilo.get("rate")
    emotion = emotion if emotion not in [None, "", "string"] else estilo.get("emotion", "")

    # Construir atributos dinámicamente
    prosody_attrs = f'pitch="{pitch}"' if pitch else ""
    if rate:
        prosody_attrs += f' rate="{rate}"'

    ssml = f"""
    <speak>
        <speechify:style emotion="{emotion}">
            <prosody {prosody_attrs}>{texto}</prosody>
        </speechify:style>
    </speak>
    """.strip()

    return ssml


def texto_a_voz_speechify(texto: str, output_path: str, voice_id: str,
                          pitch: Optional[str] = None,
                          rate: Optional[str] = None,
                          emotion: Optional[str] = None):
    ssml = construir_ssml(texto, voice_id, pitch, rate, emotion)

    response = client.tts.audio.speech(
        input=ssml,
        voice_id=voice_id,
        audio_format="mp3",
        language="es-ES",
        model="simba_multilingual"
    )

    if hasattr(response, 'audio_data'):
        audio_bytes = base64.b64decode(response.audio_data)
        with open(output_path, "wb") as f:
            f.write(audio_bytes)
        return output_path

    if isinstance(response, (bytes, bytearray)):
        with open(output_path, "wb") as f:
            f.write(response)
        return output_path

    raise ValueError("Respuesta inesperada del API de Speechify: no contiene audio")

def texto_a_voz_speechify(texto: str, output_path: str, voice_id: str, pitch=None, rate=None, emotion=None):
    ssml = construir_ssml(texto, voice_id, pitch, rate, emotion)

    response = client.tts.audio.speech(
        input=ssml,
        voice_id=voice_id,
        audio_format="mp3",
        language="es-ES",
        model="simba-multilingual"
    )

    if hasattr(response, 'audio_data'):
        audio_bytes = base64.b64decode(response.audio_data)
        with open(output_path, "wb") as f:
            f.write(audio_bytes)
        return output_path

    if isinstance(response, (bytes, bytearray)):
        with open(output_path, "wb") as f:
            f.write(response)
        return output_path

    raise ValueError("Respuesta inesperada del API de Speechify: no contiene audio")
