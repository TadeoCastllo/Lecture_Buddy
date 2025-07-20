# voice_cloning_service.py
import json
import requests

def clonar_voz_speechify(name: str, gender: str, consent: dict,
                         sample_path: str, token: str):
    url = "https://api.sws.speechify.com/v1/voices"
    headers = {"Authorization": f"Bearer {token}"}
    files = {"sample": open(sample_path, "rb")}
    data  = {
        "name": name,
        "gender": gender,
        # se espera consent como *texto* JSON
        "consent": json.dumps(consent),
    }

    resp = requests.post(url, headers=headers, data=data, files=files)
    files["sample"].close()

    
    if resp.status_code in (200, 201):
        return resp.json()     
    raise RuntimeError(f"Speechify error {resp.status_code}: {resp.text}")
