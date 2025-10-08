Chat Sentiment AI

Basitçe: Bu klasörde mesajların duygusunu (pozitif/negatif/nötr) tahmin eden küçük bir Gradio uygulaması var. Aşağıdaki adımları izleyerek çalıştırıyorum.

Gerekenler
- Python 3.10+ (3.11 önerilir)
- pip (ve tercihen venv)

Kurulum
```bash
cd /Users/batuhanacan/Desktop/chat-ai-project/ai-service

# (İsteğe bağlı) sanal ortam
python3 -m venv .venv
source .venv/bin/activate

# Gerekli paketler
pip install --upgrade pip
pip install gradio
```

Çalıştırma
```bash
python app.py
# Tarayıcıdan: http://127.0.0.1:7860
```

Programatik Çağrı (isteğe bağlı)
```python
from gradio_client import Client
client = Client("http://127.0.0.1:7860/")
result = client.predict("Bugün harika hissediyorum!", api_name="/predict")
print(result)
```

Notlar
- Port doluysa 7861 gibi farklı bir port deneyebilirim.
- Hugging Face Spaces’a açmak için `sdk: gradio` ve `app_file: app.py` yeterli olur.