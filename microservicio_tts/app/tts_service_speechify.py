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

def listar_voces_speechify():
    voces = client.tts.voices.list()
    return [{"voice_id": v.id, "name": v.display_name, "gender": v.gender} for v in voces]

def construir_ssml(texto: str, pitch: Optional[str], rate: Optional[str], emotion: Optional[str]) -> str:
    # Si se pasa "string", None, o "", se ignora
    pitch = pitch if pitch not in [None, "", "string"] else "medium"
    rate = rate if rate not in [None, "", "string"] else None
    emotion = emotion if emotion not in [None, "", "string"] else "direct"

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
    ssml = construir_ssml(texto, pitch, rate, emotion)
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
