from flask import Flask, request
from logic import *
from flask_cors import CORS
import base64

app = Flask(__name__)
CORS(app)


@app.route('/text', methods=['POST', 'GET'])
def text():
    # int.from_bytes(x, )
    # get bytes(raw, encoding) pass to decode/encode
    data_raw = request.values['b64text']
    data_encoded = base64.b64decode(data_raw)
    # e, n
    eb = base64.b64decode(request.values['e'])
    nb = base64.b64decode(request.values['n'])
    e = int_from_bytes(eb)
    n = int_from_bytes(nb)
    text_decoded = decrypt(data_encoded).decode('utf-8')
    response_text = transform_text(text_decoded)
    response = response_text.encode('utf-8')
    response_encoded = encrypt(response, e, n)
    generate_new_keys()
    return base64.b64encode(response_encoded).decode('ascii')


@app.route('/key', methods=['GET'])
def key():
    pbk = get_public_numbers()
    # b64 bytes -> str
    e = base64.b64encode(int_to_bytes(pbk.e)).decode('ascii')
    n = base64.b64encode(int_to_bytes(pbk.n)).decode('ascii')
    return "{0},{1}".format(e, n)


def transform_text(text):
    return text[::-1] + " В рот я тебя ебал."


def int_to_bytes(x):
    return x.to_bytes((x.bit_length() + 7) // 8, 'big')


def int_from_bytes(xbytes):
    return int.from_bytes(xbytes, 'big')
