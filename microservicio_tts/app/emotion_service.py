from transformers import pipeline

try:
    emotion_classifier = pipeline("text-classification", model="j-hartmann/emotion-english-distilroberta-base", top_k=None)
except Exception as e:
    print(f"Error al cargar el modelo de Hugging Face: {e}")
    emotion_classifier = None

def analizar_emocion_hf(texto: str):
    if emotion_classifier is None:
        return {
            "status": "error",
            "message": "El modelo de emociones de Hugging Face no está disponible en el servidor local."
        }
    if not texto.strip():
        return {
            "status": "error",
            "message": "El texto proporcionado está vacío."
        }
    try:
        resultados = emotion_classifier(texto)[0]
        if not resultados:
            return {
                "status": "error",
                "message": "No se detectaron emociones en el texto."
            }
        emocion_dominante = max(resultados, key=lambda x: x["score"])
        return {
            "status": "success",
            "emocion_dominante": {
                "nombre": emocion_dominante["label"],
                "valor": emocion_dominante["score"]
            },
            "emociones_completas": {r["label"]: r["score"] for r in resultados}
        }
    except Exception as e:
        return {
            "status": "error",
            "message": f"Ocurrió un error inesperado: {str(e)}"
        }
