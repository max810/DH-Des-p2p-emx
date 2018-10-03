import json
import logging

from flask import Flask, request
from websocket_server import WebsocketServer
from logic import *
# from flask_cors import CORS
# from flask_socketio import SocketIO, emit
# from flask_sockets import Sockets
import base64

usernames = dict()
server_private_keys = dict()
client_public_keys = dict()


def encode_public_keynumbers(pbkn):
    e = base64.b64encode(int_to_bytes(pbkn.e)).decode('ascii')
    n = base64.b64encode(int_to_bytes(pbkn.n)).decode('ascii')
    return e, n


def decode_public_keynumbers(eb64, nb64):
    eb = base64.b64decode(eb64)
    nb = base64.b64decode(nb64)
    e = int_from_bytes(eb)
    n = int_from_bytes(nb)
    return e, n


def int_to_bytes(x):
    return x.to_bytes((x.bit_length() + 7) // 8, 'big')


def int_from_bytes(xbytes):
    return int.from_bytes(xbytes, 'big')


def format_message(event, data):
    message = {"event_type": event, "data": data}
    return json.dumps(message)


# app = Flask(__name__)
# app.debug = True
# sockets = Sockets(app)
def on_new_client(client, server):
    print(client['id'], client['address'], sep='|')


def on_message(client, server, message):
    msg = json.loads(message)
    event = msg["event_type"]
    handler = handlers[event]
    handler(client, msg['data'])


def on_register(client, data):
    client_id = client['id']
    if client_id in usernames:
        server.send_message(client, format_message('registration_fail', {}))
        return
    username = data['username']
    eb64, nb64 = data['e'], data['n']

    usernames[client_id] = username

    e, n = decode_public_keynumbers(eb64, nb64)
    userPbk = create_public_key(e, n)
    client_public_keys[client_id] = userPbk

    server_prk = generate_new_private_key()
    server_private_keys[client_id] = server_prk
    e_serverb64, n_serverb64 = encode_public_keynumbers(
        server_prk.public_key().public_numbers()
    )

    server.send_message(
        client,
        format_message(
            'registration_success',
            {
                'e': e_serverb64,
                'n': n_serverb64
            }
        )
    )

    server.send_message_to_all(
        format_message(
            'client_joined_chat',
            {
                'username': username
            }
        )
    )


def on_check_username(client, data):
    username = data['username']
    if username in usernames.values():
        server.send_message(client, format_message('username_taken', {}))
    else:
        server.send_message(client, format_message('username_available', {}))


def on_chat_message(client, data):
    client_id = client['id']
    username = usernames[client_id]
    eb64, nb64 = data['e'], data['n']
    e, n = decode_public_keynumbers(eb64, nb64)
    client_public_keys[client_id] = create_public_key(e, n)

    message = data['message']
    message_bytes = decrypt(base64.b64decode(message), server_private_keys[client_id])

    for curr_client in server.clients:
        if curr_client['id'] not in usernames:
            continue
        curr_client_id = curr_client['id']
        curr_client_pbk = client_public_keys[curr_client_id]

        message_encrypted = encryptb64(message_bytes, curr_client_pbk).decode('ascii')

        message_data = {
            'username': username,
            'message': message_encrypted
        }
        if curr_client_id == client_id:
            new_key = generate_new_private_key()
            server_private_keys[curr_client_id] = new_key
            e_private, n_private = encode_public_keynumbers(new_key.public_key().public_numbers())
            message_data['e'] = e_private
            message_data['n'] = n_private
            server.send_message(curr_client, format_message('my_chat_message_received', message_data))
        else:
            server.send_message(curr_client, format_message('chat_message_received', message_data))


def on_client_left_chat(client, data):
    client_id = client['id']
    if client_id in usernames:
        username = usernames[client_id]

        server.send_message_to_all(
            format_message(
                'client_left_chat',
                {
                    'username': username
                }
            )
        )

        del usernames[client_id]
    if client_id in client_public_keys:
        del client_public_keys[client_id]
    if client_id in server_private_keys:
        del server_private_keys[client_id]


handlers = {
    "check_username": on_check_username,
    "register": on_register,
    "chat_message": on_chat_message,
    "client_left_chat": on_client_left_chat
}

server = WebsocketServer(5000, host='127.0.0.1', loglevel=logging.DEBUG)
server.set_fn_new_client(on_new_client)
server.set_fn_message_received(on_message)
server.set_fn_client_left(on_client_left_chat)
server.run_forever()

# CORS(app, origins='*')
# app.config['SECRET_KEY'] = 'wearenumberone'
# socketio = SocketIO(app)
#
# server_private_keys = dict()
# user_public_keys = dict()
# usernames = dict()
#
#
# #
# # @app.route('/text', methods=['POST', 'GET'])
# # def text():
# #     # int.from_bytes(x, )
# #     # get bytes(raw, encoding) pass to decode/encode
# #     data_raw = request.values['b64text']
# #     data_encoded = base64.b64decode(data_raw)
# #     # e, n
# #     eb = base64.b64decode(request.values['e'])
# #     nb = base64.b64decode(request.values['n'])
# #     e = int_from_bytes(eb)
# #     n = int_from_bytes(nb)
# #     text_decoded = decrypt(data_encoded, prk=None).decode('utf-8')
# #     response_text = transform_text(text_decoded)
# #     response = response_text.encode('utf-8')
# #     response_encoded = encrypt(response, e, n)
# #     generate_new_keys()
# #     return base64.b64encode(response_encoded).decode('ascii')
#
#
# # @socketio.on('connect')
# # def on_connect():
# #     x = request.sid
# #     emit('connect', {'e': 1, 'n': 2})
# #
#
#
# @socketio.on('check_username')
# def check_username(username):
#     if username in usernames.values():
#         emit('username_taken')
#     else:
#         emit('username_available')
#
#
# @socketio.on('connect')
# def on_connect():
#     emit('connect')
#
#
# @socketio.on('disconnect')
# def on_disconnect():
#     user_id = request.sid
#     if user_id in usernames:
#         del server_private_keys[user_id]
#         del user_public_keys[user_id]
#         del usernames[user_id]
#
#
# @socketio.on('register')
# def send_server_pbk(username, eb64, nb64):
#     if request.sid in usernames:
#         emit('registration_fail')
#         return
#     usernames[request.sid] = username
#     e, n = decode_public_keynumbers(eb64, nb64)
#     userPbk = create_public_key(e, n)
#     user_public_keys[request.sid] = userPbk
#     server_prk = generate_new_private_key()
#     server_private_keys[request.sid] = server_prk
#     e_serverb64, n_serverb64 = encode_public_keynumbers(
#         server_prk.public_key().public_numbers()
#     )
#     emit(
#         'registration_success',
#         {
#             'e': e_serverb64,
#             'n': n_serverb64
#         }
#     )
#
#
# @socketio.on('message')
# def on_message(message, e, n):
#     user_id = request.sid
#     username = usernames[user_id]
#     e, n = decode_public_keynumbers(e, n)
#     user_public_keys[user_id] = create_public_key(e, n)
#     message_bytes = decrypt(base64.b64decode(message), server_private_keys[user_id])
#     for user_id in usernames:
#         user_pbk = user_public_keys[user_id]
#         message_encrypted = encryptb64(message_bytes, user_pbk)
#         data = {
#             'user': user_id,
#             'message': message_encrypted.decode('ascii')
#         }
#         if user_id == user_id:
#             new_key = generate_new_private_key()
#             server_private_keys[user_id] = new_key
#             e_private, n_private = encode_public_keynumbers(user_pbk.public_numbers())
#             data['e'] = e_private
#             data['n'] = n_private
#             emit('my_message', data, room=user_id)
#         else:
#             emit('message', data, room=user_id)
#
# @sockets.route('/test')
# def test(ws):
#     while True:
#         message = ws.receive()
#         ws.send(message[::-1])


#
#
# if __name__ == "main":
#     socketio.run(app, debug=True)

# if __name__ == "main":
# from gevent import pywsgi
# from geventwebsocket.handler import WebSocketHandler
#
# server = pywsgi.WSGIServer(('', 5000), app, handler_class=WebSocketHandler)
# server.serve_forever()
