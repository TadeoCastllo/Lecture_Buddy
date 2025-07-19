import whisper
import os

# Cargar el modelo una sola vez
model = whisper.load_model("base")

def transcribe_audio(file_path: str) -> str:
    result = model.transcribe(file_path)
    return result["text"]
