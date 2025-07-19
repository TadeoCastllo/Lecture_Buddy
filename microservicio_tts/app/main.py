from fastapi import FastAPI, UploadFile, File, HTTPException, Form
from fastapi.responses import JSONResponse, FileResponse
from pydantic import BaseModel
import os, uuid,json, shutil 
from app.whisper_service import transcribe_audio
from app.tts_service import texto_a_voz, listar_voces
from app.tts_service_speechify import listar_voces_speechify, texto_a_voz_speechify
from app.emotion_service import analizar_emocion_hf
from app.voice_cloning_service import clonar_voz_speechify
from typing import Optional

app = FastAPI()

UPLOAD_DIR = "temp_uploads"
OUTPUT_DIR = "audios_generados"
os.makedirs(UPLOAD_DIR, exist_ok=True)
os.makedirs(OUTPUT_DIR, exist_ok=True)

class SpeechifyTTSRequest(BaseModel):
    text: str
    voice_id: str
    audio_format: str = "mp3"
    language: str = "es-ES"
    model: str = "simba-multilingual"
    pitch: Optional[str] = None  # <-- Nuevo
    rate: Optional[str] = None   # <-- Nuevo
    emotion: Optional[str] = None  # Por si también se quiere sobrescribir
    
class TTSRequest(BaseModel):
    text: str
    voice_id: str = "WOY6pnQ1WCg0mrOZ54lM"
    model_id: str = "eleven_monolingual_v1"
    stability: float = 0.5
    similarity_boost: float = 0.75
    style: float = 0.0
    speed: float = 1.0

class EmotionRequest(BaseModel):
    text: str

class SpeechifyVoiceCloneRequest(BaseModel):
    name: str
    gender: str
    consent: dict

@app.post("/transcribe_audio")
async def transcribe_audio_endpoint(file: UploadFile = File(...)):
    try:
        file_location = os.path.join(UPLOAD_DIR, file.filename)
        
        # Guardar temporalmente el archivo
        with open(file_location, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)

        # Transcribir
        text = transcribe_audio(file_location)

        # Eliminar el archivo después
        os.remove(file_location)

        return JSONResponse(content={"text": text}) 

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/text_to_speech")
async def text_to_speech_endpoint(req: TTSRequest):
    try:
        output_path = os.path.join(OUTPUT_DIR, f"{uuid.uuid4().hex}.mp3")
        texto_a_voz(
            texto=req.text,
            output_path=output_path,
            voice_id=req.voice_id,
            model_id=req.model_id,
            stability=req.stability,
            similarity_boost=req.similarity_boost,
            style=req.style,
            speed=req.speed
        )
        return FileResponse(output_path, media_type="audio/mpeg", filename=os.path.basename(output_path))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/voices")
def get_voices():
    try:
        voces = listar_voces()
        return {"voices": voces}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    
@app.get("/voices_speechify")
def get_voices_speechify():
    try:
        voces = listar_voces_speechify()
        return {"voices": voces}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    
@app.post("/text_to_speech_speechify")
async def text_to_speech_speechify_endpoint(req: SpeechifyTTSRequest):
    try:
        output_filename = f"{uuid.uuid4().hex}.mp3"
        output_path = os.path.join(OUTPUT_DIR, output_filename)

        texto_a_voz_speechify(
            texto=req.text,
            output_path=output_path,
            voice_id=req.voice_id,
            pitch=req.pitch,
            rate=req.rate,
            emotion=req.emotion
        )

        return FileResponse(output_path, media_type="audio/mpeg", filename=output_filename)

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/analizar_emocion_hf")
async def analizar_emocion_hf_endpoint(payload: EmotionRequest):
    resultado = analizar_emocion_hf(payload.text)
    if resultado["status"] == "error":
        raise HTTPException(status_code=400, detail=resultado["message"])
    return resultado

@app.post("/clone_voice_speechify")
async def clone_voice_speechify(
    name: str        = Form(...),        # ⇽ campos de texto
    gender: str      = Form(...),
    consent: str     = Form(...),        # JSON como string
    sample: UploadFile = File(...),      # archivo de audio
):
    try:
        # Guardamos el audio temporalmente
        tmp_path = os.path.join(
            UPLOAD_DIR, f"{uuid.uuid4().hex}_{sample.filename}"
        )
        with open(tmp_path, "wb") as f:
            f.write(await sample.read())

        # Cargamos el token
        token = os.getenv("SPEECHIFY_API_KEY")
        if not token:
            raise HTTPException(500, "SPEECHIFY_API_KEY no definido")

        # Parseamos consent a dict
        try:
            consent_dict = json.loads(consent)
        except json.JSONDecodeError:
            raise HTTPException(400, "El campo 'consent' debe ser JSON válido")

        resultado = clonar_voz_speechify(
            name=name,
            gender=gender,
            consent=consent_dict,
            sample_path=tmp_path,
            token=token,
        )
        return resultado

    except RuntimeError as e:             # error del servicio
        raise HTTPException(502, str(e))
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)