import os
from sentence_transformers import SentenceTransformer
from flask import Flask, request, jsonify

app = Flask(__name__)
MAX_TEXT_CHARS = 20000  # model answers have no length limit, so this must stay generous
model = SentenceTransformer('sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2')


@app.route('/embed', methods=['POST'])
def embed():
    data = request.get_json(silent=True) or {}
    texts = data.get('texts', [])

    # The API always sends exactly two texts; anything else is refused so one request can't exhaust memory/CPU
    if (not isinstance(texts, list) or len(texts) != 2
            or not all(isinstance(t, str) and 0 < len(t) <= MAX_TEXT_CHARS for t in texts)):
        return jsonify({'error': f'Provide "texts" as exactly two strings of 1 to {MAX_TEXT_CHARS} characters'}), 400

    embeddings = model.encode(texts)
    return jsonify({'embeddings': [e.tolist() for e in embeddings]})


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'healthy'})


if __name__ == '__main__':
    # Local runs listen on 127.0.0.1 only. The Docker image sets SBERT_HOST=0.0.0.0 and publishes the port on 127.0.0.1.
    app.run(host=os.environ.get('SBERT_HOST', '127.0.0.1'), port=8000)