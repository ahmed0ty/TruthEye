import hmac
import os

import gradio as gr
import uvicorn
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from sentence_transformers import SentenceTransformer

MODEL_NAME = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
MAX_TEXT_CHARS = 20000
SERVICE_TOKEN = os.environ.get("SBERT_TOKEN", "")

if not SERVICE_TOKEN:
    print("WARNING: SBERT_TOKEN is not set; /embed will refuse all requests")

model = SentenceTransformer(MODEL_NAME)
api = FastAPI()


@api.get("/health")
def health():
    return {"status": "healthy"}


@api.post("/embed")
async def embed(request: Request):
    provided = request.headers.get("X-SBERT-Token", "")
    if not SERVICE_TOKEN or not hmac.compare_digest(provided, SERVICE_TOKEN):
        return JSONResponse({"error": "Unauthorized"}, status_code=401)

    try:
        data = await request.json()
    except Exception:
        data = {}
    texts = data.get("texts", []) if isinstance(data, dict) else []

    # The API always sends exactly two texts; anything else is refused
    if (not isinstance(texts, list) or len(texts) != 2
            or not all(isinstance(t, str) and 0 < len(t) <= MAX_TEXT_CHARS for t in texts)):
        return JSONResponse(
            {"error": f'Provide "texts" as exactly two strings of 1 to {MAX_TEXT_CHARS} characters'},
            status_code=400,
        )

    embeddings = model.encode(texts)
    return {"embeddings": [e.tolist() for e in embeddings]}


with gr.Blocks() as demo:
    gr.Markdown("TruthEye SBERT service. Use POST /embed and GET /health.")

# Gradio UI at "/", API routes above take precedence
app = gr.mount_gradio_app(api, demo, path="/")

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=7860)
