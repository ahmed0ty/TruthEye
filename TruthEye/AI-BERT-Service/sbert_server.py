from sentence_transformers import SentenceTransformer
from flask import Flask, request, jsonify

app = Flask(__name__)
model = SentenceTransformer('sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2')


@app.route('/embed', methods=['POST'])
def embed():
    data = request.get_json(silent=True) or {}
    texts = data.get('texts', [])

    if not isinstance(texts, list) or not texts or not all(isinstance(t, str) for t in texts):
        return jsonify({'error': 'Provide "texts" as a non-empty list of strings'}), 400

    embeddings = model.encode(texts)
    return jsonify({'embeddings': [e.tolist() for e in embeddings]})


@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'healthy'})


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8000)